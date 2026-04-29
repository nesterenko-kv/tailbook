using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking.Contracts;
using Tailbook.Modules.Booking.Domain;

namespace Tailbook.Modules.Booking.Application;

public sealed class ClientPortalBookingQueries(
    AppDbContext dbContext,
    BookingManagementQueries bookingManagementQueries,
    BookingQuoteQueries bookingQuoteQueries,
    IPetSummaryReadService petSummaryReadService,
    IPetQuoteProfileService petQuoteProfileService,
    ICatalogQuoteResolver catalogQuoteResolver,
    ICatalogOfferReadService catalogOfferReadService
)
{
    public Task<ErrorOr<BookingRequestDetailView>> CreateMyBookingRequestAsync(ClientPortalActor actor,
        CreateClientBookingRequestCommand command, CancellationToken cancellationToken)
    {
        return bookingManagementQueries.CreateBookingRequestAsync(
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
            try
            {
                var resolution = await catalogQuoteResolver.ResolveAsync(
                    pet,
                    [new QuotePreviewCatalogItem(offer.Id, offer.OfferType)],
                    cancellationToken);

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
            catch (InvalidOperationException)
            {
                // Skip offers that are currently not bookable for this pet because
                // they do not have a published version or matching active price/duration rules.
            }

        return result
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.OfferType)
            .ToArray();
    }

    public async Task<ErrorOr<QuotePreviewView>> PreviewMyQuoteAsync(ClientPortalActor actor, PreviewQuoteCommand command,
        CancellationToken cancellationToken)
    {
        var pet = await petQuoteProfileService.GetPetAsync(command.PetId, cancellationToken);
        if (pet is null || pet.ClientId != actor.ClientId)
        {
            return Error.NotFound("Booking.PetNotFound", "Pet does not exist.");
        }

        return await bookingQuoteQueries.PreviewQuoteAsync(command, actor.UserId.ToString("D"), cancellationToken);
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
        var appointment = await bookingManagementQueries.GetAppointmentAsync(appointmentId, cancellationToken);
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

public sealed record CreateClientBookingRequestCommand(
    Guid PetId,
    string? Notes,
    IReadOnlyCollection<PreferredTimeWindowCommand> PreferredTimes,
    IReadOnlyCollection<CreateClientBookingRequestItemCommand> Items);

public sealed record CreateClientBookingRequestItemCommand(Guid OfferId, string? ItemType, string? RequestedNotes);

public sealed record ClientBookableOfferView(
    Guid Id,
    string OfferType,
    string DisplayName,
    string Currency,
    decimal PriceAmount,
    int ServiceMinutes,
    int ReservedMinutes);

public sealed record ClientAppointmentSummaryView(
    Guid Id,
    Guid PetId,
    string PetName,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string Status,
    IReadOnlyCollection<string> ItemLabels);

public sealed record ClientAppointmentItemView(
    Guid Id,
    string ItemType,
    string OfferDisplayName,
    decimal PriceAmount,
    int ServiceMinutes,
    int ReservedMinutes);

public sealed record ClientAppointmentDetailView(
    Guid Id,
    Guid? BookingRequestId,
    Guid PetId,
    string BreedName,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string Status,
    IReadOnlyCollection<ClientAppointmentItemView> Items,
    decimal TotalAmount,
    int ServiceMinutes,
    int ReservedMinutes,
    string? CancellationReasonCode,
    string? CancellationNotes,
    DateTime? CancelledAtUtc);
