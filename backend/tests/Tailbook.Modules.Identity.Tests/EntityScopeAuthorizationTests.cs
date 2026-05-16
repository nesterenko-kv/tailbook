using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.Api.Tests;
using Tailbook.Api.Tests.Factories;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Customer.Domain.Aggregates;
using Xunit;

namespace Tailbook.Modules.Identity.Tests;

public sealed class EntityScopeAuthorizationTests(RealDbWebApplicationFactory factory) : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Client_detail_read_writes_access_audit_entry()
    {
        var clientId = await SeedClientAsync();
        var token = await factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");

        using var client = factory.CreateClient();
        RealDbWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync($"/api/admin/clients/{clientId:D}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await TestApiHelpers.WaitUntilAsync(async () =>
        {
            var auditResponse = await client.GetAsync($"/api/admin/audit/access?resourceType=client&resourceId={clientId:D}");
            Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);

            var payload = await auditResponse.Content.ReadFromJsonAsync<AccessAuditResponse>();
            return payload?.Items.Any(x => x.ActionCode == "READ_DETAIL" && x.ResourceId == clientId.ToString("D")) == true;
        }, "Client detail access audit entry was not persisted.");
    }

    [Fact]
    public async Task Entity_scope_service_records_access_audit()
    {
        using var scope = factory.Services.CreateScope();
        var entityScopeService = scope.ServiceProvider.GetRequiredService<IEntityScopeService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var adminUserId = await GetAdminUserIdAsync(scope);

        var resourceId = Guid.NewGuid().ToString("D");
        var result = await entityScopeService.VerifyAccessAsync("test_resource", resourceId, adminUserId, default);

        Assert.False(result.IsError);

        await TestApiHelpers.WaitUntilAsync(async () =>
        {
            return await dbContext.Set<AccessAuditEntry>()
                .AnyAsync(x => x.ResourceType == "test_resource" && x.ResourceId == resourceId && x.ActionCode == "READ_DETAIL");
        }, "Entity scope service access audit entry was not persisted.");
    }

    [Fact]
    public async Task Entity_scope_service_denies_unknown_user()
    {
        using var scope = factory.Services.CreateScope();
        var entityScopeService = scope.ServiceProvider.GetRequiredService<IEntityScopeService>();
        var unknownUserId = Guid.NewGuid();

        var result = await entityScopeService.VerifyAccessAsync("test_resource", Guid.NewGuid().ToString("D"), unknownUserId, default);

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Code == "Scope.Denied");
    }

    [Fact]
    public async Task Non_global_scope_user_can_only_access_scoped_client()
    {
        var scopedClientId = await SeedClientAsync("Scoped Client");
        var otherClientId = await SeedClientAsync("Other Client");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var email = $"scoped-{Guid.NewGuid():N}@test.local";
        var user = new IdentityUser
        {
            Id = Guid.NewGuid(),
            SubjectId = $"usr_{Guid.NewGuid():N}",
            Email = email,
            NormalizedEmail = IdentityUseCases.NormalizeEmail(email),
            DisplayName = "Scoped User",
            PasswordHash = passwordHasher.Hash("ScopedP@ss123"),
            Status = "active",
            CreatedAt = timeProvider.GetUtcNow(),
            UpdatedAt = timeProvider.GetUtcNow()
        };
        dbContext.Set<IdentityUser>().Add(user);
        await dbContext.SaveChangesAsync();

        var adminRole = await dbContext.Set<IdentityRole>().SingleAsync(x => x.Code == "admin");
        dbContext.Set<UserRoleAssignment>().Add(new UserRoleAssignment
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RoleId = adminRole.Id,
            ScopeType = "Client",
            ScopeId = scopedClientId.ToString("D"),
            AssignedAt = timeProvider.GetUtcNow()
        });
        await dbContext.SaveChangesAsync();

        using var httpClient = factory.CreateClient();
        var loginResponse = await httpClient.PostAsJsonAsync("/api/identity/auth/login", new { email, password = "ScopedP@ss123" });
        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();
        RealDbWebApplicationFactory.SetBearer(httpClient, loginPayload!.AccessToken);

        var deniedResponse = await httpClient.GetAsync($"/api/admin/clients/{otherClientId:D}");
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);

        var allowedResponse = await httpClient.GetAsync($"/api/admin/clients/{scopedClientId:D}");
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
    }

    private async Task<Guid> GetAdminUserIdAsync(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var adminUser = await dbContext.Set<IdentityUser>().SingleAsync(x => x.NormalizedEmail == "ADMIN@TEST.LOCAL");
        return adminUser.Id;
    }

    private async Task<Guid> SeedClientAsync(string displayName = "Test Client")
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var client = new Client
        {
            Id = Guid.NewGuid(),
            DisplayName = displayName,
            Status = "active",
            Notes = null,
            CreatedAt = timeProvider.GetUtcNow(),
            UpdatedAt = timeProvider.GetUtcNow()
        };

        dbContext.Set<Client>().Add(client);
        await dbContext.SaveChangesAsync();

        return client.Id;
    }

    private sealed class AccessTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
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
