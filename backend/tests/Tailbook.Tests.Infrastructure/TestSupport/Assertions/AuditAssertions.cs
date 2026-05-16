using Tailbook.Api.Tests.TestSupport.Http;
using Xunit;

namespace Tailbook.Api.Tests.TestSupport.Assertions;

public static class AuditAssertions
{
    extension(HttpClient client)
    {
        public async Task AssertAuditEntryEventuallyExistsAsync(
            string moduleCode,
            string entityType,
            string entityId,
            string actionCode,
            string failureMessage)
        {
            var auditUrl = $"/api/admin/audit/{moduleCode}/{entityType}/{entityId}";
            var response = await client.GetAsync(auditUrl);
            response.ShouldBeOk();

            var results = await response.ReadRequiredJsonAsync<IReadOnlyCollection<AuditEntryResult>>();
            Assert.Contains(results, x => x.ActionCode == actionCode);
        }
    }
}

public sealed record AuditEntryResult
{
    public string ActionCode { get; set; } = string.Empty;
}
