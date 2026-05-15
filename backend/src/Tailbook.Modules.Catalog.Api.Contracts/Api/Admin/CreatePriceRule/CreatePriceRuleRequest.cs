namespace Tailbook.Modules.Catalog.Api.Admin.CreatePriceRule;

public sealed class CreatePriceRuleRequest
{
    public Guid RuleSetId { get; set; }
    public Guid OfferId { get; set; }
    public int Priority { get; set; } = 100;
    public decimal FixedAmount { get; set; }
    public string Currency { get; set; } = "UAH";
    public Guid? AnimalTypeId { get; set; }
    public Guid? BreedId { get; set; }
    public Guid? BreedGroupId { get; set; }
    public Guid? CoatTypeId { get; set; }
    public Guid? SizeCategoryId { get; set; }
}