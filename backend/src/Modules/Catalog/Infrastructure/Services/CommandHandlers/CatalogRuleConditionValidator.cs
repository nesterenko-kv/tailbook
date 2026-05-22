using ErrorOr;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Catalog.Infrastructure.Services.CommandHandlers;

internal static class CatalogRuleConditionValidator
{
    public static async Task<ErrorOr<RuleConditionInput>> ValidateAsync(
        RuleConditionInput condition,
        IPetTaxonomyValidationService petTaxonomyValidationService,
        CancellationToken cancellationToken)
    {
        if (condition.AnimalTypeId is not null)
        {
            if (!await petTaxonomyValidationService.AnimalTypeExistsAsync(condition.AnimalTypeId.Value, cancellationToken))
            {
                return Error.NotFound("Catalog.AnimalTypeNotFound", "Animal type does not exist.");
            }
        }

        if (condition.BreedId is not null)
        {
            if (!await petTaxonomyValidationService.BreedExistsAsync(condition.BreedId.Value, cancellationToken))
            {
                return Error.NotFound("Catalog.BreedNotFound", "Breed does not exist.");
            }
        }

        if (condition.BreedGroupId is not null)
        {
            if (!await petTaxonomyValidationService.BreedGroupExistsAsync(condition.BreedGroupId.Value, cancellationToken))
            {
                return Error.NotFound("Catalog.BreedGroupNotFound", "Breed group does not exist.");
            }
        }

        if (condition.CoatTypeId is not null)
        {
            if (!await petTaxonomyValidationService.CoatTypeExistsAsync(condition.CoatTypeId.Value, cancellationToken))
            {
                return Error.NotFound("Catalog.CoatTypeNotFound", "Coat type does not exist.");
            }
        }

        if (condition.SizeCategoryId is not null)
        {
            if (!await petTaxonomyValidationService.SizeCategoryExistsAsync(condition.SizeCategoryId.Value, cancellationToken))
            {
                return Error.NotFound("Catalog.SizeCategoryNotFound", "Size category does not exist.");
            }
        }

        return condition;
    }
}
