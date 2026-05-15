using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Booking.Infrastructure.Services;

public sealed class ClientPortalBookingReadService(
    AppDbContext dbContext,
    IBookingManagementReadService bookingManagementReadService,
    BookingQuoteReadService bookingQuoteReadService,
    IPetSummaryReadService petSummaryReadService,
    IPetQuoteProfileService petQuoteProfileService,
    ICatalogQuoteResolver catalogQuoteResolver,
    ICatalogOfferReadService catalogOfferReadService
) : IClientPortalBookingReadService
{
    public async Task<IReadOnlyCollection<ClientBookableOfferView>?> ListMyBookableOffersAsync(Guid clientId,
        Guid petId, CancellationToken cancellationToken)
    {
        var pet = await petQuoteProfileService.GetPetAsync(petId, cancellationToken);
        if (pet is null || pet.ClientId != clientId) return null;

        var offers = (await catalogOfferReadService.ListActiveOffersAsync(cancellationToken))
            .Where(x => !string.Equals(x.OfferType, "AddOn", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (offers.Length == 0) return [];

        var result = new List<ClientBookableOfferView>();
        foreach (var offer in offers)
        {
            var resolutionResult = await catalogQuoteResolver.ResolveAsync(
                pet,
                [new QuotePreviewCatalogItem(offer.Id, offer.OfferType)],
                cancellationToken);
            if (resolutionResult.IsError)
            {
                continue;
            }

            var resolution = resolutionResult.Value;
            var item = resolution.Items.Single();
            result.Add(new ClientBookableOfferView(
                item.OfferId,
                item.OfferType,
                item.DisplayName,
                resolution.Currency,
                item.PriceAmount,
                item.ServiceMinutes,
                item.ReservedMinutes));
        }

        return result
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.OfferType)
            .ToArray();
    }

    public async Task<ErrorOr<QuotePreviewView>> PreviewMyQuoteAsync(ClientPortalActor actor, PreviewQuoteQuery command,
        CancellationToken cancellationToken)
    {
        var pet = await petQuoteProfileService.GetPetAsync(command.PetId, cancellationToken);
        if (pet is null || pet.ClientId != actor.ClientId)
        {
            return Error.NotFound("Booking.PetNotFound", "Pet does not exist.");
        }

        return await bookingQuoteReadService.PreviewQuoteAsync(command, actor.UserId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ClientAppointmentSummaryView>> ListMyAppointmentsAsync(Guid clientId,
        DateTimeOffset? from, CancellationToken cancellationToken)
    {
        var petMap = (await petSummaryReadService.ListPetSummariesByClientAsync(clientId, cancellationToken))
            .ToDictionary(x => x.Id);
        if (petMap.Count == 0)
        {
            return [];
        }

        var petIds = petMap.Keys.ToArray();
        var query = dbContext.Set<Appointment>()
            .AsNoTracking()
            .Where(x => petIds.Contains(x.PetId));
        if (from.HasValue)
        {
            query = query.Where(x => x.StartAt >= from.Value);
        }

        var appointments = await query
            .OrderBy(x => x.StartAt)
            .Select(x => new
            {
                x.Id,
                x.PetId,
                x.StartAt,
                x.EndAt,
                x.Status
            })
            .ToArrayAsync(cancellationToken);
        if (appointments.Length == 0) return [];

        var appointmentIds = appointments.Select(x => x.Id).ToArray();
        var items = await dbContext.Set<AppointmentItem>()
            .AsNoTracking()
            .Where(x => appointmentIds.Contains(x.AppointmentId))
            .OrderBy(x => x.CreatedAt)
            .Select(x => new { x.AppointmentId, x.OfferDisplayNameSnapshot })
            .ToArrayAsync(cancellationToken);
        var itemLabelsByAppointment = items
            .GroupBy(x => x.AppointmentId)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<string>)x.Select(y => y.OfferDisplayNameSnapshot).ToArray());

        return appointments.Select(appointment =>
        {
            itemLabelsByAppointment.TryGetValue(appointment.Id, out var itemLabels);
            return new ClientAppointmentSummaryView(
                appointment.Id,
                appointment.PetId,
                petMap[appointment.PetId].Name,
                appointment.StartAt,
                appointment.EndAt,
                appointment.Status,
                itemLabels ?? []);
        }).ToArray();
    }

    public async Task<ClientAppointmentDetailView?> GetMyAppointmentAsync(Guid clientId, Guid appointmentId,
        CancellationToken cancellationToken)
    {
        var appointment = await bookingManagementReadService.GetAppointmentAsync(appointmentId, cancellationToken);
        if (appointment is null || appointment.Pet.ClientId != clientId) return null;

        return new ClientAppointmentDetailView(
            appointment.Id,
            appointment.BookingRequestId,
            appointment.Pet.Id,
            appointment.Pet.BreedName,
            appointment.StartAt,
            appointment.EndAt,
            appointment.Status,
            appointment.Items.Select(x => new ClientAppointmentItemView(x.Id, x.ItemType, x.OfferDisplayName,
                x.PriceAmount, x.ServiceMinutes, x.ReservedMinutes)).ToArray(),
            appointment.TotalAmount,
            appointment.ServiceMinutes,
            appointment.ReservedMinutes,
            appointment.CancellationReasonCode,
            appointment.CancellationNotes,
            appointment.CancelledAt);
    }
}
