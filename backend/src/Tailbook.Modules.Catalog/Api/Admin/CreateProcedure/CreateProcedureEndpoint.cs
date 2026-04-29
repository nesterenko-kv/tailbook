using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Catalog.Application;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateProcedure;

public sealed class CreateProcedureEndpoint(CatalogQueries catalogQueries)
    : Endpoint<CreateProcedureRequest, CreateProcedureResponse>
{
    public override void Configure()
    {
        Post("/api/admin/catalog/procedures");
        Description(x => x.WithTags("Admin Catalog"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(CreateProcedureRequest req, CancellationToken ct)
    {
        try
        {
            var procedure = await catalogQueries.CreateProcedureAsync(req.Code, req.Name, ct);
            await Send.ResponseAsync(new CreateProcedureResponse
            {
                Id = procedure.Id,
                Code = procedure.Code,
                Name = procedure.Name,
                IsActive = procedure.IsActive,
                CreatedAtUtc = procedure.CreatedAtUtc,
                UpdatedAtUtc = procedure.UpdatedAtUtc
            }, StatusCodes.Status201Created, ct);
        }
        catch (InvalidOperationException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class CreateProcedureRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class CreateProcedureRequestValidator : Validator<CreateProcedureRequest>
{
    public CreateProcedureRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateProcedureResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
