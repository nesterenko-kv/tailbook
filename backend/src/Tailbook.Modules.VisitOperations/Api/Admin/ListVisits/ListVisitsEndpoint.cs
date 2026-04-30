using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Admin.ListVisits;

public sealed class ListVisitsEndpoint(IVisitReadService visitReadService)
    : Endpoint<ListVisitsRequest, PagedResult<VisitListItemView>>
{
    public override void Configure()
    {
        Get("/api/admin/visits");
        Description(x => x.WithTags("Admin Visits"));
        PermissionsAll("visit.read");
    }

    public override async Task HandleAsync(ListVisitsRequest req, CancellationToken ct)
    {
        var status = req.VisitStatus;
        if (string.IsNullOrWhiteSpace(status) && HttpContext.Request.Query.TryGetValue("status", out var statusValues))
        {
            status = statusValues.FirstOrDefault();
        }

        var result = await visitReadService.ListVisitsAsync(
            status,
            req.FromUtc,
            req.ToUtc,
            req.GroomerId,
            req.AppointmentId,
            req.Page,
            req.PageSize,
            ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}

public sealed class ListVisitsRequest
{
    public string? VisitStatus { get; set; }

    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public Guid? GroomerId { get; set; }
    public Guid? AppointmentId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class ListVisitsRequestValidator : Validator<ListVisitsRequest>
{
    public ListVisitsRequestValidator()
    {
        RuleFor(x => x.VisitStatus).MaximumLength(64);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x).Must(x => !x.FromUtc.HasValue || !x.ToUtc.HasValue || x.ToUtc.Value > x.FromUtc.Value)
            .WithMessage("toUtc must be later than fromUtc.");
    }
}
