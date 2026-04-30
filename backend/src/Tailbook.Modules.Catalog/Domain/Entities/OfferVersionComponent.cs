namespace Tailbook.Modules.Catalog.Domain.Entities;

public sealed class OfferVersionComponent
{
    public Guid Id { get; set; }
    public Guid OfferVersionId { get; set; }
    public Guid ProcedureId { get; set; }
    public string ComponentRole { get; set; } = string.Empty;
    public int SequenceNo { get; set; }
    public bool DefaultExpected { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
