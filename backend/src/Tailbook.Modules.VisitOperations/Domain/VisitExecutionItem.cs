namespace Tailbook.Modules.VisitOperations.Domain;

public sealed class VisitExecutionItem
{
    public Guid Id { get; set; }
    public Guid VisitId { get; set; }
    public Guid AppointmentItemId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public Guid OfferId { get; set; }
    public Guid OfferVersionId { get; set; }
    public string OfferCodeSnapshot { get; set; } = string.Empty;
    public string OfferDisplayNameSnapshot { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal PriceAmountSnapshot { get; set; }
    public int ServiceMinutesSnapshot { get; set; }
    public int ReservedMinutesSnapshot { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
