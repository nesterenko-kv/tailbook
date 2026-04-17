using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Application;
using Tailbook.Modules.Identity.Contracts;
using Tailbook.Modules.Identity.Domain;

namespace Tailbook.Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestJwtIssuer = "tailbook.tests";
    private const string TestJwtAudience = "tailbook.tests.clients";
    private const string TestJwtSigningKey = "test-signing-key-that-is-at-least-32chars";

    private readonly string _databaseName = $"tailbook-tests-{Guid.NewGuid():N}";
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BootstrapAdmin:Email"] = "admin@test.local",
                ["BootstrapAdmin:Password"] = "MyV3ryC00lAdminP@ss",
                ["BootstrapAdmin:DisplayName"] = "Test Admin",

                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["Jwt:SigningKey"] = TestJwtSigningKey,
                ["Jwt:ExpirationMinutes"] = "120",
                ["Notifications:LocalFilePath"] = Path.Combine(Path.GetTempPath(), $"tailbook-test-notifications-{Guid.NewGuid():N}.log")
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();

            var dbContextOptionsConfigurationDescriptors = services
                .Where(x => x.ServiceType.IsGenericType
                            && x.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)
                            && x.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext))
                .ToList();

            foreach (var descriptor in dbContextOptionsConfigurationDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName, _databaseRoot);
                options.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            services.PostConfigure<JwtOptions>(options =>
            {
                options.Issuer = TestJwtIssuer;
                options.Audience = TestJwtAudience;
                options.SigningKey = TestJwtSigningKey;
                options.ExpirationMinutes = 120;
            });

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = TestJwtIssuer,
                    ValidAudience = TestJwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSigningKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });
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

        var roles = await dbContext.Set<IdentityRole>()
            .Where(x => roleCodes.Contains(x.Code))
            .ToListAsync();

        dbContext.Set<UserRoleAssignment>().AddRange(roles.Select(role => new UserRoleAssignment
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RoleId = role.Id,
            ScopeType = "Global",
            ScopeId = null,
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
