namespace Tailbook.Modules.Catalog.Domain;

public sealed class DurationRuleCondition
{
    public Guid Id { get; set; }
    public Guid DurationRuleId { get; set; }
    public Guid? AnimalTypeId { get; set; }
    public Guid? BreedId { get; set; }
    public Guid? BreedGroupId { get; set; }
    public Guid? CoatTypeId { get; set; }
    public Guid? SizeCategoryId { get; set; }
}
