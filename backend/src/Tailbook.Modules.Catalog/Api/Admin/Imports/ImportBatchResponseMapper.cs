namespace Tailbook.Modules.Catalog.Api.Admin.Imports;

internal static class ImportBatchResponseMapper
{
    public static ImportBatchSummaryResponse Map(ImportBatchSummaryView view) => new()
    {
        Id = view.Id,
        Domain = view.Domain,
        Status = view.Status,
        SourceName = view.SourceName,
        TotalRows = view.TotalRows,
        ValidRows = view.ValidRows,
        ErrorRows = view.ErrorRows,
        ActorUserId = view.ActorUserId,
        CreatedAt = view.CreatedAt,
        CommittedAt = view.CommittedAt
    };

    public static ImportBatchResponse Map(ImportBatchView view) => new()
    {
        Id = view.Id,
        Domain = view.Domain,
        Status = view.Status,
        SourceName = view.SourceName,
        TotalRows = view.TotalRows,
        ValidRows = view.ValidRows,
        ErrorRows = view.ErrorRows,
        ActorUserId = view.ActorUserId,
        CreatedAt = view.CreatedAt,
        CommittedAt = view.CommittedAt,
        Issues = view.Issues.Select(issue => new ImportRowIssueResponse
        {
            RowNumber = issue.RowNumber,
            Field = issue.Field,
            Code = issue.Code,
            Message = issue.Message
        }).ToArray()
    };
}
