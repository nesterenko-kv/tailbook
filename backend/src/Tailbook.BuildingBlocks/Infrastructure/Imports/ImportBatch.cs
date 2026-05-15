namespace Tailbook.BuildingBlocks.Infrastructure.Imports;

public sealed class ImportBatch
{
    private readonly List<ImportBatchRow> _rows = [];
    private readonly List<ImportBatchIssueEntity> _issues = [];

    public Guid Id { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int ErrorRows { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CommittedAt { get; set; }
    public IReadOnlyCollection<ImportBatchRow> Rows => _rows.AsReadOnly();
    public IReadOnlyCollection<ImportBatchIssueEntity> Issues => _issues.AsReadOnly();

    public static ImportBatch Create(
        Guid id,
        string domain,
        string sourceName,
        int totalRows,
        IReadOnlyCollection<ImportValidationIssue> issues,
        Guid? actorUserId,
        DateTimeOffset createdAt)
    {
        var errorRowCount = issues.Select(x => x.RowNumber).Distinct().Count();
        return new ImportBatch
        {
            Id = id,
            Domain = domain,
            Status = issues.Count == 0 ? ImportBatchStatusCodes.Validated : ImportBatchStatusCodes.Invalid,
            SourceName = string.IsNullOrWhiteSpace(sourceName) ? "upload.csv" : sourceName.Trim(),
            TotalRows = totalRows,
            ValidRows = Math.Max(0, totalRows - errorRowCount),
            ErrorRows = errorRowCount,
            ActorUserId = actorUserId,
            CreatedAt = createdAt.ToUniversalTime()
        };
    }

    public void AddRow(ImportBatchRow row) => _rows.Add(row);

    public void AddIssue(ImportBatchIssueEntity issue) => _issues.Add(issue);
}

public static class ImportBatchStatusCodes
{
    public const string Validated = "Validated";
    public const string Invalid = "Invalid";
    public const string Committed = "Committed";
}

public static class ImportDomainCodes
{
    public const string CatalogOffers = "catalog.offers";
}
