using ErrorOr;

namespace Tailbook.Modules.Catalog.Application.Catalog.Imports;

public interface ICatalogOfferImportService
{
    Task<ErrorOr<ImportBatchView>> PreviewAsync(CatalogOfferImportPreviewInput input, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ImportBatchSummaryView>> ListAsync(CancellationToken cancellationToken);

    Task<ErrorOr<string>> ExportErrorsAsync(Guid batchId, CancellationToken cancellationToken);
}

public sealed record CatalogOfferImportPreviewInput(string SourceName, string CsvContent, Guid? ActorUserId);

public sealed record ImportBatchSummaryView(
    Guid Id,
    string Domain,
    string Status,
    string SourceName,
    int TotalRows,
    int ValidRows,
    int ErrorRows,
    Guid? ActorUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CommittedAt);

public sealed record ImportBatchView(
    Guid Id,
    string Domain,
    string Status,
    string SourceName,
    int TotalRows,
    int ValidRows,
    int ErrorRows,
    Guid? ActorUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CommittedAt,
    IReadOnlyCollection<ImportRowIssueView> Issues);

public sealed record ImportRowIssueView(int RowNumber, string Field, string Code, string Message);
