using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Contracts;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class VisitOperationsAuthorizationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Groomer_cannot_access_admin_visit_endpoints()
    {
        await factory.SeedUserAsync("visit-groomer@test.local", "Visit Groomer", "Groomer123!", "groomer");
        var token = await factory.LoginAsAsync("visit-groomer@test.local", "Groomer123!");

        using var client = factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync($"/api/admin/visits/{Guid.NewGuid():D}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Visit_write_without_adjustment_permission_cannot_apply_financial_adjustment()
    {
        var email = $"visit-writer-{Guid.NewGuid():N}@test.local";
        var userId = await factory.SeedUserAsync(email, "Visit Writer", "Visit123!");
        await AssignCustomRoleAsync(userId, $"visit-writer-{Guid.NewGuid():N}", PermissionCodes.VisitRead, PermissionCodes.VisitWrite);
        var token = await factory.LoginAsAsync(email, "Visit123!");

        using var client = factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var response = await client.PostAsJsonAsync($"/api/admin/visits/{Guid.NewGuid():D}/adjustments", new
        {
            sign = 1,
            amount = 10m,
            reasonCode = "manual"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task AssignCustomRoleAsync(Guid userId, string roleCode, params string[] permissionCodes)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var permissions = await dbContext.Set<IdentityPermission>()
            .Where(x => permissionCodes.Contains(x.Code))
            .ToListAsync();

        var role = new IdentityRole
        {
            Id = Guid.NewGuid(),
            Code = roleCode,
            DisplayName = roleCode,
            IsSystem = false
        };

        dbContext.Set<IdentityRole>().Add(role);
        dbContext.Set<RolePermission>().AddRange(permissions.Select(permission => new RolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id
        }));
        dbContext.Set<UserRoleAssignment>().Add(new UserRoleAssignment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = role.Id,
            ScopeType = "Global",
            ScopeId = null,
            AssignedAt = TimeProvider.System.GetUtcNow()
        });

        await dbContext.SaveChangesAsync();
    }
}
