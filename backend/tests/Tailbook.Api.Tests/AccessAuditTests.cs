using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class AccessAuditTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AccessAuditTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Reading_sensitive_user_detail_writes_access_audit_entry()
    {
        var targetUserId = await _factory.SeedUserAsync("manager@test.local", "Manager", "Manager123!", "manager");
        var token = await _factory.LoginAsAsync("admin@test.local", "Admin12345!");

        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var detailResponse = await client.GetAsync($"/api/admin/iam/users/{targetUserId:D}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        var auditResponse = await client.GetAsync($"/api/admin/audit/access?resourceType=iam_user&resourceId={targetUserId:D}");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);

        var payload = await auditResponse.Content.ReadFromJsonAsync<AccessAuditResponse>();
        Assert.NotNull(payload);
        Assert.Contains(payload!.Items, x => x.ActionCode == "READ_DETAIL" && x.ResourceId == targetUserId.ToString("D"));
    }

    private sealed class AccessAuditResponse
    {
        public AccessAuditItem[] Items { get; set; } = [];
    }

    private sealed class AccessAuditItem
    {
        public string ResourceId { get; set; } = string.Empty;
        public string ActionCode { get; set; } = string.Empty;
    }
}
