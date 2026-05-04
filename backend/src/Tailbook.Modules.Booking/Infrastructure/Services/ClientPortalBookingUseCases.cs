using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking.Contracts;

namespace Tailbook.Modules.Booking.Infrastructure.Services;

public sealed class ClientPortalBookingUseCases(
    AppDbContext dbContext,
    BookingManagementUseCases bookingManagementUseCases,
    BookingQuoteReadService bookingQuoteReadService,
    IPetSummaryReadService petSummaryReadService,
    IPetQuoteProfileService petQuoteProfileService,
    ICatalogQuoteResolver catalogQuoteResolver,
    ICatalogOfferReadService catalogOfferReadService
) : IClientPortalBookingReadService
{
    public Task<ErrorOr<BookingRequestDetailView>> CreateMyBookingRequestAsync(ClientPortalActor actor,
        CreateClientBookingRequestCommand command, CancellationToken cancellationToken)
    {
        return bookingManagementUseCases.CreateBookingRequestAsync(
            new CreateBookingRequestCommand(
                actor.ClientId,
                command.PetId,
                actor.ContactPersonId,
                BookingChannelCodes.ClientPortal,
                command.Notes,
                command.PreferredTimes.Select(x => new PreferredTimeWindowCommand(x.StartAtUtc, x.EndAtUtc, x.Label))
                    .ToArray(),
                command.Items.Select(x => new CreateBookingRequestItemCommand(x.OfferId, x.ItemType, x.RequestedNotes))
                    .ToArray()),
            actor.UserId.ToString("D"),
            cancellationToken);
    }

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

        return await bookingQuoteReadService.PreviewQuoteAsync(command, actor.UserId.ToString("D"), cancellationToken);
    }

    public async Task<IReadOnlyCollection<ClientAppointmentSummaryView>> ListMyAppointmentsAsync(Guid clientId,
        DateTime? fromUtc, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Appointment>().AsQueryable();
        if (fromUtc.HasValue) query = query.Where(x => x.StartAtUtc >= fromUtc.Value);

        var appointments = await query.OrderBy(x => x.StartAtUtc).ToListAsync(cancellationToken);
        if (appointments.Count == 0) return [];

        var petIds = appointments.Select(x => x.PetId).Distinct().ToArray();
        var pets = await Task.WhenAll(
            petIds.Select(x => petSummaryReadService.GetPetSummaryAsync(x, cancellationToken)));
        var petMap = pets.Where(x => x is not null).ToDictionary(x => x!.Id, x => x!);
        var filtered = appointments.Where(x => petMap.TryGetValue(x.PetId, out var pet) && pet.ClientId == clientId)
            .ToArray();

        if (filtered.Length == 0) return [];

        var appointmentIds = filtered.Select(x => x.Id).ToArray();
        var items = await dbContext.Set<AppointmentItem>()
            .Where(x => appointmentIds.Contains(x.AppointmentId))
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return filtered.Select(appointment => new ClientAppointmentSummaryView(
                appointment.Id,
                appointment.PetId,
                petMap[appointment.PetId].Name,
                appointment.StartAtUtc,
                appointment.EndAtUtc,
                appointment.Status,
                items.Where(x => x.AppointmentId == appointment.Id).Select(x => x.OfferDisplayNameSnapshot).ToArray()))
            .ToArray();
    }

    public async Task<ClientAppointmentDetailView?> GetMyAppointmentAsync(Guid clientId, Guid appointmentId,
        CancellationToken cancellationToken)
    {
        var appointment = await bookingManagementUseCases.GetAppointmentAsync(appointmentId, cancellationToken);
        if (appointment is null || appointment.Pet.ClientId != clientId) return null;

        return new ClientAppointmentDetailView(
            appointment.Id,
            appointment.BookingRequestId,
            appointment.Pet.Id,
            appointment.Pet.BreedName,
            appointment.StartAtUtc,
            appointment.EndAtUtc,
            appointment.Status,
            appointment.Items.Select(x => new ClientAppointmentItemView(x.Id, x.ItemType, x.OfferDisplayName,
                x.PriceAmount, x.ServiceMinutes, x.ReservedMinutes)).ToArray(),
            appointment.TotalAmount,
            appointment.ServiceMinutes,
            appointment.ReservedMinutes,
            appointment.CancellationReasonCode,
            appointment.CancellationNotes,
            appointment.CancelledAtUtc);
    }
}
