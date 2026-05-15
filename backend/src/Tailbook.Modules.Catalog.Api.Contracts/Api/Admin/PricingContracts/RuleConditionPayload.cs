namespace Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

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