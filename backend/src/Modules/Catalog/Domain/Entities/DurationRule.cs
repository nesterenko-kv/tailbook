namespace Tailbook.Modules.Catalog.Domain.Entities;

public sealed class DurationRule
{
    public Guid Id { get; set; }
    public Guid RuleSetId { get; set; }
    public Guid OfferId { get; set; }
    public int Priority { get; set; }
    public int SpecificityScore { get; set; }
    public int BaseMinutes { get; set; }
    public int BufferBeforeMinutes { get; set; }
    public int BufferAfterMinutes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DurationRuleCondition Condition { get; set; } = null!;

    internal static DurationRule Create(
        Guid id,
        Guid ruleSetId,
        Guid offerId,
        int priority,
        int baseMinutes,
        int bufferBeforeMinutes,
        int bufferAfterMinutes,
        Guid? animalTypeId,
        Guid? breedId,
        Guid? breedGroupId,
        Guid? coatTypeId,
        Guid? sizeCategoryId,
        DateTimeOffset utcNow)
    {
        var condition = DurationRuleCondition.Create(Guid.NewGuid(), id, animalTypeId, breedId, breedGroupId, coatTypeId, sizeCategoryId);
        return new DurationRule
        {
            Id = id,
            RuleSetId = ruleSetId,
            OfferId = offerId,
            Priority = priority,
            SpecificityScore = condition.SpecificityScore,
            BaseMinutes = baseMinutes,
            BufferBeforeMinutes = bufferBeforeMinutes,
            BufferAfterMinutes = bufferAfterMinutes,
            CreatedAt = utcNow.ToUniversalTime(),
            Condition = condition
        };
    }

    internal bool HasEquivalentCondition(Guid? animalTypeId, Guid? breedId, Guid? breedGroupId, Guid? coatTypeId, Guid? sizeCategoryId)
    {
        return Condition.Matches(animalTypeId, breedId, breedGroupId, coatTypeId, sizeCategoryId);
    }
}
