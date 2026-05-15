using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Catalog.Infrastructure.Services.CommandHandlers;

public sealed class CreateCatalogProcedureCommandHandler(AppDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<CreateCatalogProcedureCommand, ErrorOr<ProcedureView>>
{
    public async Task<ErrorOr<ProcedureView>> ExecuteAsync(CreateCatalogProcedureCommand command, CancellationToken cancellationToken)
    {
        var normalizedCode = NormalizeCode(command.Code);
        if (normalizedCode.IsError)
        {
            return normalizedCode.Errors;
        }

        var displayName = command.Name.Trim();

        var duplicate = await dbContext.Set<ProcedureCatalogItem>()
            .AnyAsync(x => x.Code == normalizedCode.Value, cancellationToken);
        if (duplicate)
        {
            return Error.Conflict("Catalog.ProcedureCodeExists", $"A procedure with code '{normalizedCode.Value}' already exists.");
        }

        var utcNow = timeProvider.GetUtcNow();
        var entity = new ProcedureCatalogItem
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode.Value,
            Name = displayName,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        dbContext.Set<ProcedureCatalogItem>().Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new ProcedureView(entity.Id, entity.Code, entity.Name, entity.IsActive, entity.CreatedAt, entity.UpdatedAt);
    }

    private static ErrorOr<string> NormalizeCode(string code)
    {
        var normalized = code.Trim().ToUpperInvariant().Replace(' ', '_');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Error.Validation("Catalog.CodeRequired", "Code is required.");
        }

        return normalized;
    }
}
