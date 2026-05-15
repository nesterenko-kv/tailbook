namespace Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

public class PriceRuleSetResponseBase
{
    public Guid Id { get; set; }
    public int VersionNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public PriceRuleResponseBase[] Rules { get; set; } = [];

    public static PriceRuleSetResponseBase FromView(PriceRuleSetView view)
        => FromView<PriceRuleSetResponseBase>(view);

    protected static TResponse FromView<TResponse>(PriceRuleSetView view)
        where TResponse : PriceRuleSetResponseBase, new()
    {
        return new TResponse
        {
            Id = view.Id,
            VersionNo = view.VersionNo,
            Status = view.Status,
            ValidFrom = view.ValidFrom,
            ValidTo = view.ValidTo,
            CreatedAt = view.CreatedAt,
            PublishedAt = view.PublishedAt,
            Rules = view.Rules.Select(PriceRuleResponseBase.FromView).ToArray()
        };
    }
}
