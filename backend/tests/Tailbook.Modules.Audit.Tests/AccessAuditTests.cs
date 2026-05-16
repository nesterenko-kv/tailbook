using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class AccessAuditTests(RealDbWebApplicationFactory factory) : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Reading_sensitive_user_detail_writes_access_audit_entry()
    {
        var targetUserId = await factory.SeedUserAsync("manager@test.local", "Manager", "Manager123!", "manager");
        var token = await factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");

        using var client = factory.CreateClient();
        RealDbWebApplicationFactory.SetBearer(client, token);

        var detailResponse = await client.GetAsync($"/api/admin/iam/users/{targetUserId:D}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        await TestApiHelpers.WaitUntilAsync(async () =>
        {
            var auditResponse = await client.GetAsync($"/api/admin/audit/access?resourceType=iam_user&resourceId={targetUserId:D}");
            Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);

            var payload = await auditResponse.Content.ReadFromJsonAsync<AccessAuditResponse>();
            return payload?.Items.Any(x => x.ActionCode == "READ_DETAIL" && x.ResourceId == targetUserId.ToString("D")) == true;
        }, "Sensitive user detail access audit entry was not persisted.");
    }

    [Fact]
    public async Task Assigning_roles_writes_audit_trail_entry()
    {
        var targetUserId = await factory.SeedUserAsync($"role-audit-{Guid.NewGuid():N}@test.local", "Role Audit", "Manager123!");
        var token = await factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");

        using var client = factory.CreateClient();
        RealDbWebApplicationFactory.SetBearer(client, token);

        var response = await client.PostAsJsonAsync($"/api/admin/iam/users/{targetUserId:D}/roles", new
        {
            id = targetUserId,
            roleCodes = new[] { "manager" }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await TestApiHelpers.WaitUntilAsync(async () =>
        {
            using var waitScope = factory.Services.CreateScope();
            var waitDbContext = waitScope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await waitDbContext.Set<AuditEntry>()
                .AnyAsync(x => x.ModuleCode == "identity" && x.EntityType == "iam_user" && x.EntityId == targetUserId.ToString("D") && x.ActionCode == "ASSIGN_ROLES");
        }, "Assign roles audit trail entry was not persisted.");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditEntries = await dbContext.Set<AuditEntry>()
            .Where(x => x.ModuleCode == "identity" && x.EntityType == "iam_user" && x.EntityId == targetUserId.ToString("D"))
            .ToListAsync();

        Assert.Contains(auditEntries, x => x.ActionCode == "ASSIGN_ROLES" && x.AfterJson != null && x.AfterJson.Contains("manager", StringComparison.OrdinalIgnoreCase));
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
