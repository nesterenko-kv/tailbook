namespace Tailbook.Modules.Catalog.Application.Catalog.Models;

public sealed record OfferVersionComponentView(Guid Id, Guid OfferVersionId, Guid ProcedureId, string ProcedureCode, string ProcedureName, string ComponentRole, int SequenceNo, bool DefaultExpected, DateTimeOffset CreatedAt);