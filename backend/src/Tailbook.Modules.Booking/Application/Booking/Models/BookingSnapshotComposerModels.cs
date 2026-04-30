namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record AppointmentCompositionResult(
    Guid? ClientId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    decimal TotalAmount,
    int ServiceMinutes,
    int ReservedMinutes,
    IReadOnlyCollection<AppointmentItemComposition> Items);

public sealed class AppointmentItemComposition
{
    public Guid OfferId { get; set; }
    public Guid OfferVersionId { get; set; }
    public string OfferCode { get; set; } = string.Empty;
    public string OfferType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public PriceSnapshot PriceSnapshot { get; set; } = null!;
    public IReadOnlyCollection<PriceSnapshotLineView> PriceLines { get; set; } = [];
    public DurationSnapshot DurationSnapshot { get; set; } = null!;
    public IReadOnlyCollection<DurationSnapshotLineView> DurationLines { get; set; } = [];
}
