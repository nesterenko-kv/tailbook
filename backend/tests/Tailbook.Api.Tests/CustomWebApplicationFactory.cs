using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Application;
using Tailbook.Modules.Identity.Contracts;
using Tailbook.Modules.Identity.Domain;

namespace Tailbook.Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BootstrapAdmin:Email"] = "admin@test.local",
                ["BootstrapAdmin:Password"] = "Admin12345!",
                ["BootstrapAdmin:DisplayName"] = "Test Admin",
                ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32chars"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase($"tailbook-tests-{Guid.NewGuid():N}"));
        });
    }

    public async Task<string> LoginAsAsync(string email, string password)
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/identity/auth/login", new
        {
            email,
            password
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginResponseEnvelope>();
        return payload!.AccessToken;
    }

    public async Task<Guid> SeedUserAsync(string email, string displayName, string password, params string[] roleCodes)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();

        var user = new IdentityUser
        {
            Id = Guid.NewGuid(),
            SubjectId = $"usr_{Guid.NewGuid():N}",
            Email = email,
            NormalizedEmail = IdentityQueries.NormalizeEmail(email),
            DisplayName = displayName,
            PasswordHash = passwordHasher.Hash(password),
            Status = UserStatusCodes.Active,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.Set<IdentityUser>().Add(user);
        await dbContext.SaveChangesAsync();

        var roles = await dbContext.Set<IdentityRole>().Where(x => roleCodes.Contains(x.Code)).ToListAsync();
        dbContext.Set<UserRoleAssignment>().AddRange(roles.Select(role => new UserRoleAssignment
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RoleId = role.Id,
            ScopeType = "Global",
            AssignedAtUtc = DateTime.UtcNow
        }));

        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    public static void SetBearer(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private sealed class LoginResponseEnvelope
    {
        public string AccessToken { get; set; } = string.Empty;
    }
}
