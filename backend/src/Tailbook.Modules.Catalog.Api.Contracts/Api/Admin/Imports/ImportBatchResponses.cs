namespace Tailbook.Modules.Catalog.Api.Admin.Imports;

public class ImportBatchSummaryResponse
{
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
}

public sealed class ImportBatchResponse : ImportBatchSummaryResponse
{
    public ImportRowIssueResponse[] Issues { get; set; } = [];
}

public sealed class ImportRowIssueResponse
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
