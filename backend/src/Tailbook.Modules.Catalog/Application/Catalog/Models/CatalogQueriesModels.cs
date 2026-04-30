namespace Tailbook.Modules.Catalog.Application.Catalog.Models;

public sealed record ProcedureView(Guid Id, string Code, string Name, bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record OfferListItemView(Guid Id, string Code, string OfferType, string DisplayName, bool IsActive, int VersionCount, bool HasPublishedVersion, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record OfferDetailView(Guid Id, string Code, string OfferType, string DisplayName, bool IsActive, IReadOnlyCollection<OfferVersionView> Versions, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record OfferVersionView(Guid Id, Guid OfferId, int VersionNo, string Status, DateTime ValidFromUtc, DateTime? ValidToUtc, string? PolicyText, string? ChangeNote, DateTime CreatedAtUtc, DateTime? PublishedAtUtc, IReadOnlyCollection<OfferVersionComponentView> Components);
public sealed record OfferVersionComponentView(Guid Id, Guid OfferVersionId, Guid ProcedureId, string ProcedureCode, string ProcedureName, string ComponentRole, int SequenceNo, bool DefaultExpected, DateTime CreatedAtUtc);
