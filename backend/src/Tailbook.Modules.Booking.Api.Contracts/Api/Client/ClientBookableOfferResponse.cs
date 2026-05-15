namespace Tailbook.Modules.Booking.Api.Client;

public sealed class ClientBookableOfferResponse
{
    public Guid Id { get; set; }
    public string OfferType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal PriceAmount { get; set; }
    public int ServiceMinutes { get; set; }
    public int ReservedMinutes { get; set; }
}