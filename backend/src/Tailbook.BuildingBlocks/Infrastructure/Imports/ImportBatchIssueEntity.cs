namespace Tailbook.BuildingBlocks.Infrastructure.Imports;

public sealed class ImportBatchIssueEntity
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
