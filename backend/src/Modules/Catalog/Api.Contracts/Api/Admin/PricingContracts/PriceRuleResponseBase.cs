namespace Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

public class PriceRuleResponseBase
{
    public Guid Id { get; set; }
    public Guid RuleSetId { get; set; }
    public Guid OfferId { get; set; }
    public string OfferCode { get; set; } = string.Empty;
    public string OfferDisplayName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int SpecificityScore { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public decimal FixedAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public RuleConditionPayload Condition { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }

    public static PriceRuleResponseBase FromView(PriceRuleView view)
        => FromView<PriceRuleResponseBase>(view);

    protected static TResponse FromView<TResponse>(PriceRuleView view)
        where TResponse : PriceRuleResponseBase, new()
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
            ActionType = view.ActionType,
            FixedAmount = view.FixedAmount,
            Currency = view.Currency,
            Condition = RuleConditionPayload.FromView(view.Condition),
            CreatedAt = view.CreatedAt
        };
    }
}
