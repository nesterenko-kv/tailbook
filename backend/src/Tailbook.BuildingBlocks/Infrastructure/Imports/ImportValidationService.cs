namespace Tailbook.BuildingBlocks.Infrastructure.Imports;

public sealed class ImportValidationService
{
    public ImportValidationResult ValidatePetRows(
        IReadOnlyCollection<PetImportRow> rows,
        PetTaxonomyReferenceData taxonomy)
    {
        var issues = new List<ImportValidationIssue>();
        AddDuplicateExternalIdIssues(rows.Select(x => (x.RowNumber, x.ExternalId)), issues);

        foreach (var row in rows)
        {
            ValidateRowNumber(row.RowNumber, issues);
            Require(row.RowNumber, nameof(row.ExternalId), row.ExternalId, issues);
            Require(row.RowNumber, nameof(row.Name), row.Name, issues);
            Require(row.RowNumber, nameof(row.AnimalTypeCode), row.AnimalTypeCode, issues);
            Require(row.RowNumber, nameof(row.BreedCode), row.BreedCode, issues);
            ValidateOptionalPositiveDecimal(row.RowNumber, nameof(row.WeightKg), row.WeightKg, issues);
            ValidateReference(row.RowNumber, nameof(row.AnimalTypeCode), row.AnimalTypeCode, taxonomy.AnimalTypeCodes, issues);
            ValidateReference(row.RowNumber, nameof(row.BreedCode), row.BreedCode, taxonomy.BreedCodes, issues);
            ValidateReference(row.RowNumber, nameof(row.CoatTypeCode), row.CoatTypeCode, taxonomy.CoatTypeCodes, issues);
            ValidateReference(row.RowNumber, nameof(row.SizeCategoryCode), row.SizeCategoryCode, taxonomy.SizeCategoryCodes, issues);
        }

        return new ImportValidationResult(issues);
    }

    public ImportValidationResult ValidateCatalogOfferRows(IReadOnlyCollection<CatalogOfferImportRow> rows)
    {
        var issues = new List<ImportValidationIssue>();
        AddDuplicateExternalIdIssues(rows.Select(x => (x.RowNumber, x.ExternalId)), issues);

        foreach (var row in rows)
        {
            ValidateRowNumber(row.RowNumber, issues);
            Require(row.RowNumber, nameof(row.ExternalId), row.ExternalId, issues);
            Require(row.RowNumber, nameof(row.Code), row.Code, issues);
            Require(row.RowNumber, nameof(row.DisplayName), row.DisplayName, issues);
            ValidateRequiredPositiveDecimal(row.RowNumber, nameof(row.PriceAmount), row.PriceAmount, issues);
            ValidateRequiredPositiveInt(row.RowNumber, nameof(row.ServiceMinutes), row.ServiceMinutes, issues);
            ValidateRequiredPositiveInt(row.RowNumber, nameof(row.ReservedMinutes), row.ReservedMinutes, issues);

            if (row.ServiceMinutes.HasValue && row.ReservedMinutes.HasValue && row.ReservedMinutes.Value < row.ServiceMinutes.Value)
            {
                issues.Add(new ImportValidationIssue(row.RowNumber, nameof(row.ReservedMinutes), "duration.reserved_less_than_service", "Reserved minutes must be greater than or equal to service minutes."));
            }
        }

        return new ImportValidationResult(issues);
    }

    private static void AddDuplicateExternalIdIssues(IEnumerable<(int RowNumber, string? ExternalId)> rows, List<ImportValidationIssue> issues)
    {
        foreach (var duplicate in rows
                     .Where(x => !string.IsNullOrWhiteSpace(x.ExternalId))
                     .GroupBy(x => x.ExternalId!.Trim(), StringComparer.OrdinalIgnoreCase)
                     .Where(x => x.Count() > 1))
        {
            foreach (var row in duplicate)
            {
                issues.Add(new ImportValidationIssue(row.RowNumber, "ExternalId", "identifier.duplicate", $"External id '{duplicate.Key}' appears more than once in the import batch."));
            }
        }
    }

    private static void ValidateRowNumber(int rowNumber, List<ImportValidationIssue> issues)
    {
        if (rowNumber <= 0)
        {
            issues.Add(new ImportValidationIssue(rowNumber, "RowNumber", "row.malformed", "Row number must be greater than zero."));
        }
    }

    private static void Require(int rowNumber, string field, string? value, List<ImportValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(new ImportValidationIssue(rowNumber, field, "field.required", $"{field} is required."));
        }
    }

    private static void ValidateReference(int rowNumber, string field, string? value, IReadOnlySet<string> allowedValues, List<ImportValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!allowedValues.Contains(value.Trim()))
        {
            issues.Add(new ImportValidationIssue(rowNumber, field, "reference.invalid", $"{field} '{value}' is not in the known reference data."));
        }
    }

    private static void ValidateOptionalPositiveDecimal(int rowNumber, string field, decimal? value, List<ImportValidationIssue> issues)
    {
        if (value.HasValue && value.Value < 0)
        {
            issues.Add(new ImportValidationIssue(rowNumber, field, "number.negative", $"{field} cannot be negative."));
        }
    }

    private static void ValidateRequiredPositiveDecimal(int rowNumber, string field, decimal? value, List<ImportValidationIssue> issues)
    {
        if (!value.HasValue)
        {
            issues.Add(new ImportValidationIssue(rowNumber, field, "field.required", $"{field} is required."));
            return;
        }

        if (value.Value < 0)
        {
            issues.Add(new ImportValidationIssue(rowNumber, field, "number.negative", $"{field} cannot be negative."));
        }
    }

    private static void ValidateRequiredPositiveInt(int rowNumber, string field, int? value, List<ImportValidationIssue> issues)
    {
        if (!value.HasValue)
        {
            issues.Add(new ImportValidationIssue(rowNumber, field, "field.required", $"{field} is required."));
            return;
        }

        if (value.Value <= 0)
        {
            issues.Add(new ImportValidationIssue(rowNumber, field, "number.not_positive", $"{field} must be greater than zero."));
        }
    }
}

public sealed record ImportValidationIssue(int RowNumber, string Field, string Code, string Message);

public sealed record ImportValidationResult(IReadOnlyCollection<ImportValidationIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}

public sealed record PetImportRow(
    int RowNumber,
    string? ExternalId,
    string? Name,
    string? AnimalTypeCode,
    string? BreedCode,
    string? CoatTypeCode,
    string? SizeCategoryCode,
    decimal? WeightKg);

public sealed record PetTaxonomyReferenceData(
    IReadOnlySet<string> AnimalTypeCodes,
    IReadOnlySet<string> BreedCodes,
    IReadOnlySet<string> CoatTypeCodes,
    IReadOnlySet<string> SizeCategoryCodes);

public sealed record CatalogOfferImportRow(
    int RowNumber,
    string? ExternalId,
    string? Code,
    string? DisplayName,
    decimal? PriceAmount,
    int? ServiceMinutes,
    int? ReservedMinutes);
