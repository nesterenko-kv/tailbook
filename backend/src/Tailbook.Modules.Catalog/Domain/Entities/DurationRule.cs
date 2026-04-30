namespace Tailbook.Modules.Catalog.Domain.Entities;

public sealed class DurationRule
{
    public Guid Id { get; set; }
    public Guid RuleSetId { get; set; }
    public Guid OfferId { get; set; }
    public int Priority { get; set; }
    public int SpecificityScore { get; set; }
    public int BaseMinutes { get; set; }
    public int BufferBeforeMinutes { get; set; }
    public int BufferAfterMinutes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
