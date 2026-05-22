namespace Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

public class DurationRuleSetResponseBase
{
    public Guid Id { get; set; }
    public int VersionNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DurationRuleResponseBase[] Rules { get; set; } = [];

    public static DurationRuleSetResponseBase FromView(DurationRuleSetView view)
        => FromView<DurationRuleSetResponseBase>(view);

    protected static TResponse FromView<TResponse>(DurationRuleSetView view)
        where TResponse : DurationRuleSetResponseBase, new()
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
            Rules = view.Rules.Select(DurationRuleResponseBase.FromView).ToArray()
        };
    }
}
