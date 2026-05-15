namespace Tailbook.Modules.Catalog.Application.Catalog.Models;

public sealed record ProcedureView(Guid Id, string Code, string Name, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);