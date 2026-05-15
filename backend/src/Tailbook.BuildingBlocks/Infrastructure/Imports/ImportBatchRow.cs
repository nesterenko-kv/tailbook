namespace Tailbook.BuildingBlocks.Infrastructure.Imports;

public sealed class ImportBatchRow
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public int RowNumber { get; set; }
    public string? ExternalId { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
}
