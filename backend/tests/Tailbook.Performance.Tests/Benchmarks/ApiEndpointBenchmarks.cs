using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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

namespace Tailbook.Performance.Tests.Benchmarks;

[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
[MemoryDiagnoser]
public class ApiEndpointBenchmarks : IDisposable
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _adminToken = string.Empty;

    private const string TestJwtIssuer = "tailbook.perf";
    private const string TestJwtAudience = "tailbook.perf.clients";
    private const string TestJwtSigningKey = "perf-test-signing-key-that-is-at-least-32chars";
    private const string AdminEmail = "admin@perf.local";
    private const string AdminPassword = "PerfAdminP@ss123";

    [GlobalSetup]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("PerformanceTest");

                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["BootstrapAdmin:Email"] = AdminEmail,
                        ["BootstrapAdmin:Password"] = AdminPassword,
                        ["BootstrapAdmin:DisplayName"] = "Perf Admin",
                        ["Jwt:Issuer"] = TestJwtIssuer,
                        ["Jwt:Audience"] = TestJwtAudience,
                        ["Jwt:SigningKey"] = TestJwtSigningKey,
                        ["Jwt:ExpirationMinutes"] = "120",
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
                        options.UseInMemoryDatabase("TailbookPerf");
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
            });

        _client = _factory.CreateClient();

        var loginResponse = _client.PostAsJsonAsync("/api/identity/auth/login", new
        {
            Email = AdminEmail,
            Password = AdminPassword
        }).GetAwaiter().GetResult();

        loginResponse.EnsureSuccessStatusCode();

        var loginPayload = loginResponse.Content
            .ReadFromJsonAsync<LoginResponseEnvelope>()
            .GetAwaiter().GetResult();

        if (loginPayload?.AccessToken != null)
        {
            _adminToken = loginPayload.AccessToken;
        }
    }

    [Benchmark]
    public async Task Post_Login_Admin()
    {
        using var response = await _client.PostAsJsonAsync("/api/identity/auth/login", new
        {
            Email = AdminEmail,
            Password = AdminPassword
        });
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    public async Task Get_CurrentUser()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        _factory.Dispose();
        _client.Dispose();
    }

    private sealed class LoginResponseEnvelope
    {
        public string AccessToken { get; set; } = string.Empty;
    }
}
