using System.Net.Http.Json;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Models;
using Xunit;

namespace Tailbook.Api.Tests.TestSupport.Assertions;

public static class AuditAssertions
{
    extension(HttpClient client)
    {
        public Task AssertAuditEntryEventuallyExistsAsync(
            string moduleCode,
            string entityType,
            object entityId,
            string actionCode,
            string? failureMessage = null)
            => TestApiHelpers.WaitUntilAsync(async () =>
                {
                    var auditResponse = await client.GetAsync($"/api/admin/audit?moduleCode={moduleCode}&entityType={entityType}&entityId={entityId}");
                    auditResponse.ShouldBeOk();
                    var audit = await auditResponse.Content.ReadFromJsonAsync<AuditTrailEnvelope>();
                    return audit?.Items.Any(x => x.ActionCode == actionCode) == true;
                }, failureMessage ?? $"{actionCode} audit entry was not persisted.");

        public Task AssertAccessAuditEntryEventuallyExistsAsync(
            string resourceType,
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
