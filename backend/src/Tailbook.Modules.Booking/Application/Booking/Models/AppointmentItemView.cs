namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record AppointmentItemView(Guid Id, string ItemType, Guid OfferId, Guid OfferVersionId, string OfferCode, string OfferDisplayName, int Quantity, Guid PriceSnapshotId, Guid DurationSnapshotId, decimal PriceAmount, int ServiceMinutes, int ReservedMinutes);