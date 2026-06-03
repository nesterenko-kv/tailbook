using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Domain;
using Tailbook.Modules.Identity.Domain.Aggregates;
using Tailbook.Modules.Identity.Domain.Entities;
using Tailbook.Modules.Identity.Infrastructure.Services;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Tailbook.Api.Tests.Factories;

public sealed class RealDbWebApplicationFactory : WebApplicationFactory<Program>
{
    private PostgreSqlContainer? _postgres;
    private RedisContainer? _redis;
    private readonly string _databaseName = $"tailbook_test_{Guid.NewGuid():N}";
    public const int TestMaxFailedLoginAttempts = 3;
    private bool _containersDisposed;
    private bool _factoryDisposed;

    private async Task EnsureContainersAsync()
    {
        if (_containersDisposed || _postgres is not null)
            return;
        _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        _redis = new RedisBuilder("redis:7-alpine").Build();
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());
        await _postgres.ExecScriptAsync($"""
                                         CREATE DATABASE "{_databaseName}"
                                         """);
    }

    private async Task DisposeContainersAsync()
    {
        if (_containersDisposed)
            return;
        _containersDisposed = true;
        var pg = _postgres;
        var r = _redis;
        if (pg is not null || r is not null)
        {
            await Task.WhenAll(
                (pg?.DisposeAsync().AsTask() ?? Task.CompletedTask),
                (r?.DisposeAsync().AsTask() ?? Task.CompletedTask));
        }
    }

    public string RedisConnectionString => _redis!.GetConnectionString();

    public string PostgresConnectionString
    {
        get
        {
            var builder = new NpgsqlConnectionStringBuilder(_postgres!.GetConnectionString())
            {
                Database = _databaseName
            };
            return builder.ConnectionString;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        EnsureContainersAsync().GetAwaiter().GetResult();
        var pgConnectionString = PostgresConnectionString;
        var redisConnectionString = RedisConnectionString;

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Main"] = pgConnectionString,
            ["ConnectionStrings:Redis"] = redisConnectionString,
            ["BootstrapAdmin:Email"] = "admin@test.local",
            ["BootstrapAdmin:Password"] = "MyV3ryC00lAdminP@ss",
            ["BootstrapAdmin:DisplayName"] = "Test Admin",
            ["Jwt:Issuer"] = "tailbook.tests",
            ["Jwt:Audience"] = "tailbook.tests.clients",
            ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32chars",
            ["Jwt:ExpirationMinutes"] = "120",
            ["LoginThrottling:MaxFailedAttempts"] = "3",
            ["LoginThrottling:FailureWindowMinutes"] = "15",
            ["LoginThrottling:LockoutMinutes"] = "15",
            ["PasswordReset:ExpirationMinutes"] = "30",
            ["PasswordReset:TokenBytes"] = "32",
            ["PasswordReset:ResetUrlBase"] = "http://localhost:3002/reset-password",
            ["SensitivePayloadProtection:Key"] = "test-sensitive-payload-key-that-is-at-least-32chars",
            ["Notifications:LocalFilePath"] = Path.Combine(Path.GetTempPath(), $"tailbook-test-notifications-{Guid.NewGuid():N}.log"),
            ["Audit:QueueCapacity"] = "100",
            ["Audit:BatchSize"] = "1",
            ["Audit:FlushIntervalMilliseconds"] = "10",
            ["Audit:MaxWriteRetries"] = "1",
            ["Audit:RetryDelayMilliseconds"] = "1"
        }));

        builder.ConfigureServices(services =>
        {
            services.Configure<HostOptions>(o =>
                o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

            services.RemoveAll<NpgsqlDataSource>();

            services.AddSingleton<NpgsqlDataSource>(_ =>
            {
                var connStringBuilder = new NpgsqlConnectionStringBuilder(pgConnectionString)
                {
                    MaxPoolSize = 5,
                    ConnectionIdleLifetime = 5,
                    ConnectionPruningInterval = 5
                };

                var dataSourceBuilder = new NpgsqlDataSourceBuilder(connStringBuilder.ConnectionString)
                {
                    Name = "tailbook_tests"
                };

                return dataSourceBuilder.Build();
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
            NormalizedEmail = IdentityUseCases.NormalizeEmail(email),
            DisplayName = displayName,
            PasswordHash = passwordHasher.Hash(password),
            Status = UserStatusCodes.Active,
            CreatedAt = TimeProvider.System.GetUtcNow(),
            UpdatedAt = TimeProvider.System.GetUtcNow()
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
            AssignedAt = TimeProvider.System.GetUtcNow()
        }));

        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    public static void SetBearer(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_factoryDisposed)
            return;
        _factoryDisposed = true;
        await base.DisposeAsync();
        await DisposeContainersAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (_factoryDisposed)
            return;
        _factoryDisposed = true;
        base.Dispose(disposing);
    }

    private sealed class LoginResponseEnvelope
    {
        public string AccessToken { get; set; } = string.Empty;
    }
}
