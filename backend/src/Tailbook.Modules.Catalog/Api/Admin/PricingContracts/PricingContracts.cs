
namespace Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

public class PriceRuleSetResponseBase
{
    public Guid Id { get; set; }
    public int VersionNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public PriceRuleResponseBase[] Rules { get; set; } = [];

    public static PriceRuleSetResponseBase FromView(PriceRuleSetView view)
    {
        return new PriceRuleSetResponseBase
        {
            Id = view.Id,
            VersionNo = view.VersionNo,
            Status = view.Status,
            ValidFromUtc = view.ValidFromUtc,
            ValidToUtc = view.ValidToUtc,
            CreatedAtUtc = view.CreatedAtUtc,
            PublishedAtUtc = view.PublishedAtUtc,
            Rules = view.Rules.Select(PriceRuleResponseBase.FromView).ToArray()
        };
    }
}

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
    public DateTime CreatedAtUtc { get; set; }

    public static PriceRuleResponseBase FromView(PriceRuleView view)
    {
        return new PriceRuleResponseBase
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
            CreatedAtUtc = view.CreatedAtUtc
        };
    }
}

public class DurationRuleSetResponseBase
{
    public Guid Id { get; set; }
    public int VersionNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public DurationRuleResponseBase[] Rules { get; set; } = [];

    public static DurationRuleSetResponseBase FromView(DurationRuleSetView view)
    {
        return new DurationRuleSetResponseBase
        {
            Id = view.Id,
            VersionNo = view.VersionNo,
            Status = view.Status,
            ValidFromUtc = view.ValidFromUtc,
            ValidToUtc = view.ValidToUtc,
            CreatedAtUtc = view.CreatedAtUtc,
            PublishedAtUtc = view.PublishedAtUtc,
            Rules = view.Rules.Select(DurationRuleResponseBase.FromView).ToArray()
        };
    }
}

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
    public DateTime CreatedAtUtc { get; set; }

    public static DurationRuleResponseBase FromView(DurationRuleView view)
    {
        return new DurationRuleResponseBase
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
            CreatedAtUtc = view.CreatedAtUtc
        };
    }
}

public class RuleConditionPayload
{
    public Guid? AnimalTypeId { get; set; }
    public Guid? BreedId { get; set; }
    public Guid? BreedGroupId { get; set; }
    public Guid? CoatTypeId { get; set; }
    public Guid? SizeCategoryId { get; set; }

    public static RuleConditionPayload FromView(RuleConditionView view)
    {
        return new RuleConditionPayload
        {
            AnimalTypeId = view.AnimalTypeId,
            BreedId = view.BreedId,
            BreedGroupId = view.BreedGroupId,
            CoatTypeId = view.CoatTypeId,
            SizeCategoryId = view.SizeCategoryId
        };
    }
}
