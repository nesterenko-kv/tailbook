namespace Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

public class DurationRuleResponseBase
{
    public Guid Id { get; set; }
    public Guid RuleSetId { get; set; }
    public Guid OfferId { get; set; }
    public string OfferCode { get; set; } = string.Empty;
    public string OfferDisplayName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int SpecificityScore { get; set; }
    public int BaseMinutes { get; set; }
    public int BufferBeforeMinutes { get; set; }
    public int BufferAfterMinutes { get; set; }
    public RuleConditionPayload Condition { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }

    public static DurationRuleResponseBase FromView(DurationRuleView view)
        => FromView<DurationRuleResponseBase>(view);

    protected static TResponse FromView<TResponse>(DurationRuleView view)
        where TResponse : DurationRuleResponseBase, new()
    {
        return new TResponse
        {
            Id = view.Id,
            RuleSetId = view.RuleSetId,
            OfferId = view.OfferId,
            OfferCode = view.OfferCode,
            OfferDisplayName = view.OfferDisplayName,
            Priority = view.Priority,
            SpecificityScore = view.SpecificityScore,
            BaseMinutes = view.BaseMinutes,
            BufferBeforeMinutes = view.BufferBeforeMinutes,
            BufferAfterMinutes = view.BufferAfterMinutes,
            Condition = RuleConditionPayload.FromView(view.Condition),
            CreatedAt = view.CreatedAt
        };
    }
}
