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
    public int SpecificityScore => new[] { AnimalTypeId, BreedId, BreedGroupId, CoatTypeId, SizeCategoryId }.Count(x => x.HasValue);

    internal static PriceRuleCondition Create(
        Guid id,
        Guid priceRuleId,
        Guid? animalTypeId,
        Guid? breedId,
        Guid? breedGroupId,
        Guid? coatTypeId,
        Guid? sizeCategoryId)
    {
        return new PriceRuleCondition
        {
            Id = id,
            PriceRuleId = priceRuleId,
            AnimalTypeId = animalTypeId,
            BreedId = breedId,
            BreedGroupId = breedGroupId,
            CoatTypeId = coatTypeId,
            SizeCategoryId = sizeCategoryId
        };
    }

    internal bool Matches(Guid? animalTypeId, Guid? breedId, Guid? breedGroupId, Guid? coatTypeId, Guid? sizeCategoryId)
    {
        return AnimalTypeId == animalTypeId &&
               BreedId == breedId &&
               BreedGroupId == breedGroupId &&
               CoatTypeId == coatTypeId &&
               SizeCategoryId == sizeCategoryId;
    }
}
