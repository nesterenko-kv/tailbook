using System.Globalization;
using System.Text;
using System.Text.Json;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Imports;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Catalog.Infrastructure.Services;

public sealed class CatalogOfferImportService(
    AppDbContext dbContext,
    IAuditTrailService auditTrailService,
    TimeProvider timeProvider) : ICatalogOfferImportService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ErrorOr<ImportBatchView>> PreviewAsync(CatalogOfferImportPreviewInput input, CancellationToken cancellationToken)
    {
        var parseResult = ParseCatalogOfferRows(input.CsvContent);
        if (parseResult.IsError)
        {
            return parseResult.Errors;
        }

        var rows = parseResult.Value;
        var validator = new ImportValidationService();
        var issues = validator.ValidateCatalogOfferRows(rows).Issues.ToList();
        await AddExistingCodeIssuesAsync(rows, issues, cancellationToken);

        var batch = ImportBatch.Create(
            Guid.NewGuid(),
            ImportDomainCodes.CatalogOffers,
            input.SourceName,
            rows.Count,
            issues,
            input.ActorUserId,
            timeProvider.GetUtcNow());

        foreach (var row in rows)
        {
            batch.AddRow(new ImportBatchRow
            {
                Id = Guid.NewGuid(),
                BatchId = batch.Id,
                RowNumber = row.RowNumber,
                ExternalId = row.ExternalId,
                PayloadJson = JsonSerializer.Serialize(row, JsonOptions)
            });
        }

        foreach (var issue in issues)
        {
            batch.AddIssue(new ImportBatchIssueEntity
            {
                Id = Guid.NewGuid(),
                BatchId = batch.Id,
                RowNumber = issue.RowNumber,
                Field = issue.Field,
                Code = issue.Code,
                Message = issue.Message
            });
        }

        dbContext.Set<ImportBatch>().Add(batch);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync(
            "catalog",
            "ImportBatch",
            batch.Id.ToString("D"),
            "catalog.import.previewed",
            input.ActorUserId,
            null,
            JsonSerializer.Serialize(new { batch.Domain, batch.Status, batch.TotalRows, batch.ValidRows, batch.ErrorRows }, JsonOptions),
            cancellationToken);

        return Map(batch);
    }

    public async Task<IReadOnlyCollection<ImportBatchSummaryView>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Set<ImportBatch>()
            .AsNoTracking()
            .Where(x => x.Domain == ImportDomainCodes.CatalogOffers)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .Select(x => new ImportBatchSummaryView(
                x.Id,
                x.Domain,
                x.Status,
                x.SourceName,
                x.TotalRows,
                x.ValidRows,
                x.ErrorRows,
                x.ActorUserId,
                x.CreatedAt,
                x.CommittedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ErrorOr<string>> ExportErrorsAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var batchExists = await dbContext.Set<ImportBatch>()
            .AnyAsync(x => x.Id == batchId && x.Domain == ImportDomainCodes.CatalogOffers, cancellationToken);
        if (!batchExists)
        {
            return Error.NotFound("Import.BatchNotFound", "Import batch was not found.");
        }

        var issues = await dbContext.Set<ImportBatchIssueEntity>()
            .AsNoTracking()
            .Where(x => x.BatchId == batchId)
            .OrderBy(x => x.RowNumber)
            .ThenBy(x => x.Field)
            .ToArrayAsync(cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("RowNumber,Field,Code,Message");
        foreach (var issue in issues)
        {
            builder.Append(Csv(issue.RowNumber.ToString(CultureInfo.InvariantCulture))).Append(',')
                .Append(Csv(issue.Field)).Append(',')
                .Append(Csv(issue.Code)).Append(',')
                .Append(Csv(issue.Message)).AppendLine();
        }

        return builder.ToString();
    }

    private async Task AddExistingCodeIssuesAsync(
        IReadOnlyCollection<CatalogOfferImportRow> rows,
        List<ImportValidationIssue> issues,
        CancellationToken cancellationToken)
    {
        var codes = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .Select(x => CommercialOffer.NormalizeCode(x.Code!).Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (codes.Length == 0)
        {
            return;
        }

        var existingCodes = await dbContext.Set<CommercialOffer>()
            .AsNoTracking()
            .Where(x => codes.Contains(x.Code))
            .Select(x => x.Code)
            .ToArrayAsync(cancellationToken);

        foreach (var row in rows.Where(x => !string.IsNullOrWhiteSpace(x.Code)))
        {
            var normalized = CommercialOffer.NormalizeCode(row.Code!).Value;
            if (existingCodes.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                issues.Add(new ImportValidationIssue(row.RowNumber, nameof(row.Code), "identifier.exists", $"Catalog offer code '{normalized}' already exists."));
            }
        }
    }

    private static ErrorOr<IReadOnlyCollection<CatalogOfferImportRow>> ParseCatalogOfferRows(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return Error.Validation("Import.EmptyFile", "CSV content is required.");
        }

        var lines = csvContent.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
        {
            return Error.Validation("Import.NoRows", "CSV must include a header row and at least one data row.");
        }

        var headers = ParseCsvLine(lines[0]);
        var index = headers
            .Select((header, i) => (Header: header.Trim(), Index: i))
            .ToDictionary(x => x.Header, x => x.Index, StringComparer.OrdinalIgnoreCase);
        var requiredHeaders = new[] { "ExternalId", "Code", "DisplayName", "PriceAmount", "ServiceMinutes", "ReservedMinutes" };
        var missing = requiredHeaders.Where(x => !index.ContainsKey(x)).ToArray();
        if (missing.Length > 0)
        {
            return Error.Validation("Import.MissingHeaders", $"CSV is missing required headers: {string.Join(", ", missing)}.");
        }

        var rows = new List<CatalogOfferImportRow>();
        for (var i = 1; i < lines.Length; i++)
        {
            var fields = ParseCsvLine(lines[i]);
            rows.Add(new CatalogOfferImportRow(
                i + 1,
                Get(fields, index, "ExternalId"),
                Get(fields, index, "Code"),
                Get(fields, index, "DisplayName"),
                TryDecimal(Get(fields, index, "PriceAmount")),
                TryInt(Get(fields, index, "ServiceMinutes")),
                TryInt(Get(fields, index, "ReservedMinutes"))));
        }

        return rows;
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        values.Add(current.ToString());
        return values;
    }

    private static string? Get(IReadOnlyList<string> fields, IReadOnlyDictionary<string, int> index, string name)
    {
        var position = index[name];
        return position >= fields.Count ? null : fields[position].Trim();
    }

    private static decimal? TryDecimal(string? value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static int? TryInt(string? value) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private static ImportBatchView Map(ImportBatch batch) => new(
        batch.Id,
        batch.Domain,
        batch.Status,
        batch.SourceName,
        batch.TotalRows,
        batch.ValidRows,
        batch.ErrorRows,
        batch.ActorUserId,
        batch.CreatedAt,
        batch.CommittedAt,
        batch.Issues.OrderBy(x => x.RowNumber).ThenBy(x => x.Field).Select(x => new ImportRowIssueView(x.RowNumber, x.Field, x.Code, x.Message)).ToArray());
}
