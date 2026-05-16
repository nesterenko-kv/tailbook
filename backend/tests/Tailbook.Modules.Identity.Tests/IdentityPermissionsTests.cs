using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class IdentityPermissionsTests(RealDbWebApplicationFactory factory) : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Mfa_recovery_permission_is_seeded_for_admin_role_only()
    {
        using var client = factory.CreateClient();
        RealDbWebApplicationFactory.SetBearer(client, await factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss"));

        var permissionsResponse = await client.GetAsync("/api/admin/iam/permissions");
        Assert.Equal(HttpStatusCode.OK, permissionsResponse.StatusCode);
        var permissions = await permissionsResponse.Content.ReadFromJsonAsync<PermissionItemEnvelope[]>();
        Assert.NotNull(permissions);
        Assert.Contains(permissions, x => x.Code == "iam.mfa.recovery.write");

        var rolesResponse = await client.GetAsync("/api/admin/iam/roles");
        Assert.Equal(HttpStatusCode.OK, rolesResponse.StatusCode);
        var roles = await rolesResponse.Content.ReadFromJsonAsync<RoleItemEnvelope[]>();
        Assert.NotNull(roles);
        var admin = Assert.Single(roles, x => x.Code == "admin");
        var manager = Assert.Single(roles, x => x.Code == "manager");
        Assert.Contains("iam.mfa.recovery.write", admin.PermissionCodes);
        Assert.DoesNotContain("iam.mfa.recovery.write", manager.PermissionCodes);
    }

    private sealed class PermissionItemEnvelope
    {
        public string Code { get; set; } = string.Empty;
    }

    private sealed class RoleItemEnvelope
    {
        public string Code { get; set; } = string.Empty;
        public string[] PermissionCodes { get; set; } = [];
    }
}
