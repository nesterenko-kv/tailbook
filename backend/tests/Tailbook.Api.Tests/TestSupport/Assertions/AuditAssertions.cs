using System.Net.Http.Json;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Models;

namespace Tailbook.Api.Tests.TestSupport.Assertions;

internal static class AuditAssertions
{
    extension(HttpClient client)
    {
        internal Task AssertAuditEntryEventuallyExistsAsync(string moduleCode,
            string entityType,
            Guid entityId,
            string actionCode,
            string? failureMessage = null)
            => TestApiHelpers.WaitUntilAsync(async () =>
                {
                    var auditResponse = await client.GetAsync($"/api/admin/audit?moduleCode={moduleCode}&entityType={entityType}&entityId={entityId:D}");
                    auditResponse.ShouldBeOk();
                    var audit = await auditResponse.Content.ReadFromJsonAsync<AuditTrailEnvelope>();
                    return audit?.Items.Any(x => x.ActionCode == actionCode) == true;
                }, failureMessage ?? $"{actionCode} audit entry was not persisted.");

        internal Task AssertAccessAuditEntryEventuallyExistsAsync(string resourceType,
            Guid resourceId,
            string actionCode,
            string? failureMessage = null)
            => TestApiHelpers.WaitUntilAsync(async () =>
                {
                    var auditResponse = await client.GetAsync($"/api/admin/audit/access?resourceType={resourceType}&resourceId={resourceId:D}");
                    auditResponse.ShouldBeOk();
                    var audit = await auditResponse.Content.ReadFromJsonAsync<AccessAuditEnvelope>();
                    return audit?.Items.Any(x => x.ActionCode == actionCode) == true;
                }, failureMessage ?? $"{actionCode} access audit entry was not persisted.");
    }
}
