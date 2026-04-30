using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Audit.Application.AccessAuditEntries.Queries;

namespace Tailbook.Modules.Audit.Api.Admin.ListAccessAuditEntries;

public sealed class ListAccessAuditEntriesEndpoint(
    IAccessAuditEntryQueries accessAuditEntryQueries
) : Endpoint<ListAccessAuditEntriesRequest, ListAccessAuditEntriesResponse>
{
    public override void Configure()
    {
        Get("/api/admin/audit/access");
        Description(x => x.WithTags("Audit"));
        PermissionsAll("audit.access.read");
    }

    public override async Task HandleAsync(ListAccessAuditEntriesRequest req, CancellationToken ct)
    {
        var result = await accessAuditEntryQueries.ListAsync(new ListAccessAuditEntriesQuery(req.ResourceType, req.ResourceId, req.Page, req.PageSize), ct);
        await Send.OkAsync(result.ToResponse(), ct);
    }
}
