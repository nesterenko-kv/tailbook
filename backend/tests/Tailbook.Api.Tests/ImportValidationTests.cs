using Tailbook.BuildingBlocks.Infrastructure.Imports;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class ImportValidationTests
{
    [Fact]
    public void Pet_import_validation_reports_malformed_duplicates_missing_fields_and_bad_taxonomy()
    {
        var validator = new ImportValidationService();
        var taxonomy = new PetTaxonomyReferenceData(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DOG" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SAMOYED" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DOUBLE_COAT" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "LARGE" });

        var result = validator.ValidatePetRows(
            [
                new PetImportRow(0, "pet-1", "", "DOG", "SAMOYED", "DOUBLE_COAT", "LARGE", 12m),
                new PetImportRow(2, "PET-1", "Milo", "CAT", "UNKNOWN", "WIRE", "TINY", -1m)
            ],
            taxonomy);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, x => x.Code == "row.malformed");
        Assert.Contains(result.Issues, x => x.Code == "identifier.duplicate");
        Assert.Contains(result.Issues, x => x.Field == "Name" && x.Code == "field.required");
        Assert.Contains(result.Issues, x => x.Field == "AnimalTypeCode" && x.Code == "reference.invalid");
        Assert.Contains(result.Issues, x => x.Field == "BreedCode" && x.Code == "reference.invalid");
        Assert.Contains(result.Issues, x => x.Field == "CoatTypeCode" && x.Code == "reference.invalid");
        Assert.Contains(result.Issues, x => x.Field == "SizeCategoryCode" && x.Code == "reference.invalid");
        Assert.Contains(result.Issues, x => x.Field == "WeightKg" && x.Code == "number.negative");
    }

    [Fact]
    public void Catalog_offer_import_validation_reports_invalid_prices_and_durations()
    {
        var validator = new ImportValidationService();

        var result = validator.ValidateCatalogOfferRows(
            [
                new CatalogOfferImportRow(1, "offer-1", "", "Bath", -5m, 0, 10),
                new CatalogOfferImportRow(2, "offer-2", "PKG", "Package", 100m, 60, 30)
            ]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, x => x.Field == "Code" && x.Code == "field.required");
        Assert.Contains(result.Issues, x => x.Field == "PriceAmount" && x.Code == "number.negative");
        Assert.Contains(result.Issues, x => x.Field == "ServiceMinutes" && x.Code == "number.not_positive");
        Assert.Contains(result.Issues, x => x.Field == "ReservedMinutes" && x.Code == "duration.reserved_less_than_service");
    }
}
