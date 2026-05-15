namespace Tailbook.Modules.Catalog.Application.Catalog.Models;

public sealed record RuleConditionInput(Guid? AnimalTypeId, Guid? BreedId, Guid? BreedGroupId, Guid? CoatTypeId, Guid? SizeCategoryId);