using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Admin.ListVisits;

public sealed class ListVisitsEndpoint(
    IVisitReadService visitReadService,
    IScopeAuthorizationService scopeAuthorizationService)
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
            req.Search,
            status,
            req.From,
            req.To,
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

        var value = result.Value;
        var actorUserId = User.FindFirst(TailbookClaimTypes.UserId)?.Value;
        IReadOnlyCollection<VisitListItemView> filteredItems = value.Items;
        var totalCount = value.TotalCount;

        if (Guid.TryParse(actorUserId, out var userId))
        {
            var hasGlobal = await scopeAuthorizationService.HasGlobalScopeAsync(userId, ct);
            if (!hasGlobal)
            {
                filteredItems = await ScopeFilter.ApplyAsync(
                    value.Items,
                    userId,
                    EntityScopeResourceTypes.Visit,
                    item => item.Id.ToString("D"),
                    scopeAuthorizationService,
                    ct);
                totalCount = filteredItems.Count;
            }
        }

        var pagedResult = new PagedResult<VisitListItemView>(filteredItems, value.Page, value.PageSize, totalCount);
        await Send.OkAsync(pagedResult, ct);
    }
}
