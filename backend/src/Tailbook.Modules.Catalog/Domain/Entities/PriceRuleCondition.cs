namespace Tailbook.Modules.Catalog.Domain.Entities;

public sealed class PriceRuleCondition
{
    public Guid Id { get; set; }
    public Guid PriceRuleId { get; set; }
    public Guid? AnimalTypeId { get; set; }
    public Guid? BreedId { get; set; }
    public Guid? BreedGroupId { get; set; }
    public Guid? CoatTypeId { get; set; }
    public Guid? SizeCategoryId { get; set; }
}
