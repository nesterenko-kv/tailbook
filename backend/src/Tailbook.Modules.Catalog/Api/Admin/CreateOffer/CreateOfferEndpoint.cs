using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Catalog.Application;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

public sealed class CreateOfferEndpoint(ICurrentUser currentUser, ICatalogAccessPolicy accessPolicy, CatalogQueries catalogQueries)
    : Endpoint<CreateOfferRequest, OfferResponse>
{
    public override void Configure()
    {
        Post("/api/admin/catalog/offers");
        Description(x => x.WithTags("Admin Catalog"));
    }

    public override async Task HandleAsync(CreateOfferRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanWriteCatalog(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var offer = await catalogQueries.CreateOfferAsync(req.Code, req.OfferType, req.DisplayName, ct);
            await Send.ResponseAsync(OfferResponse.Map(offer), StatusCodes.Status201Created, ct);
        }
        catch (InvalidOperationException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class CreateOfferRequest
{
    public string Code { get; set; } = string.Empty;
    public string OfferType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class CreateOfferRequestValidator : Validator<CreateOfferRequest>
{
    public CreateOfferRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.OfferType).NotEmpty().MaximumLength(32);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
    }
}

public sealed class OfferResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string OfferType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public OfferVersionResponse[] Versions { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public static OfferResponse Map(OfferDetailView view)
    {
        return new OfferResponse
        {
            Id = view.Id,
            Code = view.Code,
            OfferType = view.OfferType,
            DisplayName = view.DisplayName,
            IsActive = view.IsActive,
            Versions = view.Versions.Select(OfferVersionResponse.Map).ToArray(),
            CreatedAtUtc = view.CreatedAtUtc,
            UpdatedAtUtc = view.UpdatedAtUtc
        };
    }
}

public sealed class OfferVersionResponse
{
    public Guid Id { get; set; }
    public Guid OfferId { get; set; }
    public int VersionNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public string? PolicyText { get; set; }
    public string? ChangeNote { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public OfferVersionComponentResponse[] Components { get; set; } = [];

    public static OfferVersionResponse Map(OfferVersionView view)
    {
        return new OfferVersionResponse
        {
            Id = view.Id,
            OfferId = view.OfferId,
            VersionNo = view.VersionNo,
            Status = view.Status,
            ValidFromUtc = view.ValidFromUtc,
            ValidToUtc = view.ValidToUtc,
            PolicyText = view.PolicyText,
            ChangeNote = view.ChangeNote,
            CreatedAtUtc = view.CreatedAtUtc,
            PublishedAtUtc = view.PublishedAtUtc,
            Components = view.Components.Select(OfferVersionComponentResponse.Map).ToArray()
        };
    }
}

public sealed class OfferVersionComponentResponse
{
    public Guid Id { get; set; }
    public Guid OfferVersionId { get; set; }
    public Guid ProcedureId { get; set; }
    public string ProcedureCode { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public string ComponentRole { get; set; } = string.Empty;
    public int SequenceNo { get; set; }
    public bool DefaultExpected { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public static OfferVersionComponentResponse Map(OfferVersionComponentView view)
    {
        return new OfferVersionComponentResponse
        {
            Id = view.Id,
            OfferVersionId = view.OfferVersionId,
            ProcedureId = view.ProcedureId,
            ProcedureCode = view.ProcedureCode,
            ProcedureName = view.ProcedureName,
            ComponentRole = view.ComponentRole,
            SequenceNo = view.SequenceNo,
            DefaultExpected = view.DefaultExpected,
            CreatedAtUtc = view.CreatedAtUtc
        };
    }
}
