using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Tailbook.Api.Tests.TestSupport.Http;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class BrowserSessionCookieTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private const string SurfaceHeaderName = "X-Tailbook-Session-Surface";
    private const string CsrfHeaderName = "X-Tailbook-CSRF";
    private const string AdminRefreshCookieName = "tailbook-admin-refresh";
    private const string AdminCsrfCookieName = "tailbook-admin-csrf";
    private const string ClientRefreshCookieName = "tailbook-client-refresh";
    private const string ClientCsrfCookieName = "tailbook-client-csrf";

    [Fact]
    public async Task Identity_login_in_cookie_mode_sets_surface_cookie_and_omits_refresh_token_body()
    {
        using var cookieFactory = CreateCookieModeFactory(allowLegacyBodyTokens: false);
        using var client = cookieFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/auth/login")
        {
            Content = JsonContent.Create(new
            {
                email = "admin@test.local",
                password = "MyV3ryC00lAdminP@ss"
            })
        };
        request.Headers.Add(SurfaceHeaderName, "admin");

        var response = await client.SendAsync(request);

        response.ShouldBeOk();
        var payload = await response.ReadRequiredJsonAsync<LoginEnvelope>();
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.Null(payload.RefreshToken);

        var setCookies = GetSetCookieHeaders(response);
        Assert.Contains(setCookies, x => x.StartsWith($"{AdminRefreshCookieName}=", StringComparison.Ordinal) && x.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(setCookies, x => x.StartsWith($"{AdminCsrfCookieName}=", StringComparison.Ordinal) && !x.Contains("httponly", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Cookie_refresh_requires_csrf_and_rotates_cookie()
    {
        using var cookieFactory = CreateCookieModeFactory(allowLegacyBodyTokens: false);
        using var client = cookieFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var loginResponse = await LoginWithAdminCookieAsync(client);
        var loginCookies = GetCookieJar(loginResponse);
        var cookieHeader = BuildCookieHeader(loginCookies);

        using var missingCsrfRequest = new HttpRequestMessage(HttpMethod.Post, "/api/identity/auth/refresh")
        {
            Content = JsonContent.Create(new { })
        };
        missingCsrfRequest.Headers.Add(SurfaceHeaderName, "admin");
        missingCsrfRequest.Headers.Add("Cookie", cookieHeader);

        var missingCsrfResponse = await client.SendAsync(missingCsrfRequest);

        missingCsrfResponse.ShouldBeForbidden();
        var missingCsrfBody = await missingCsrfResponse.Content.ReadAsStringAsync();
        Assert.Contains("Identity.BrowserSessionCsrfInvalid", missingCsrfBody, StringComparison.Ordinal);

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/identity/auth/refresh")
        {
            Content = JsonContent.Create(new { })
        };
        refreshRequest.Headers.Add(SurfaceHeaderName, "admin");
        refreshRequest.Headers.Add(CsrfHeaderName, loginCookies[AdminCsrfCookieName]);
        refreshRequest.Headers.Add("Cookie", cookieHeader);

        var refreshResponse = await client.SendAsync(refreshRequest);

        refreshResponse.ShouldBeOk();
        var refreshPayload = await refreshResponse.ReadRequiredJsonAsync<LoginEnvelope>();
        Assert.False(string.IsNullOrWhiteSpace(refreshPayload.AccessToken));
        Assert.Null(refreshPayload.RefreshToken);

        var rotatedCookies = GetCookieJar(refreshResponse);
        Assert.True(rotatedCookies.ContainsKey(AdminRefreshCookieName));
        Assert.NotEqual(loginCookies[AdminRefreshCookieName], rotatedCookies[AdminRefreshCookieName]);
    }

    [Fact]
    public async Task Cookie_mode_can_still_use_legacy_body_tokens_during_migration()
    {
        using var cookieFactory = CreateCookieModeFactory(allowLegacyBodyTokens: true);
        using var client = cookieFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var loginResponse = await client.PostAsJsonAsync("/api/identity/auth/login", new
        {
            email = "admin@test.local",
            password = "MyV3ryC00lAdminP@ss"
        });

        loginResponse.ShouldBeOk();
        var loginPayload = await loginResponse.ReadRequiredJsonAsync<LoginEnvelope>();
        Assert.False(string.IsNullOrWhiteSpace(loginPayload.RefreshToken));
        Assert.DoesNotContain(GetSetCookieHeaders(loginResponse), x => x.StartsWith($"{AdminRefreshCookieName}=", StringComparison.Ordinal));

        var refreshResponse = await client.PostAsJsonAsync("/api/identity/auth/refresh", new
        {
            refreshToken = loginPayload.RefreshToken
        });

        refreshResponse.ShouldBeOk();
        var refreshPayload = await refreshResponse.ReadRequiredJsonAsync<LoginEnvelope>();
        Assert.False(string.IsNullOrWhiteSpace(refreshPayload.RefreshToken));
        Assert.NotEqual(loginPayload.RefreshToken, refreshPayload.RefreshToken);
    }

    [Fact]
    public async Task Client_register_in_cookie_mode_uses_client_surface_by_default()
    {
        using var cookieFactory = CreateCookieModeFactory(allowLegacyBodyTokens: false);
        using var client = cookieFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var email = $"client-{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/client/auth/register", new
        {
            displayName = "Cookie Client",
            firstName = "Cookie",
            lastName = "Client",
            email,
            password = "Client123!",
            phone = "555-0100",
            instagram = "cookieclient"
        });

        response.ShouldBeCreated();
        var payload = await response.ReadRequiredJsonAsync<ClientLoginEnvelope>();
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.Null(payload.RefreshToken);

        var setCookies = GetSetCookieHeaders(response);
        Assert.Contains(setCookies, x => x.StartsWith($"{ClientRefreshCookieName}=", StringComparison.Ordinal));
        Assert.Contains(setCookies, x => x.StartsWith($"{ClientCsrfCookieName}=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Cookie_revoke_clears_refresh_and_csrf_cookies()
    {
        using var cookieFactory = CreateCookieModeFactory(allowLegacyBodyTokens: false);
        using var client = cookieFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var loginResponse = await LoginWithAdminCookieAsync(client);
        var loginCookies = GetCookieJar(loginResponse);

        using var revokeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/identity/auth/revoke")
        {
            Content = JsonContent.Create(new { })
        };
        revokeRequest.Headers.Add(SurfaceHeaderName, "admin");
        revokeRequest.Headers.Add(CsrfHeaderName, loginCookies[AdminCsrfCookieName]);
        revokeRequest.Headers.Add("Cookie", BuildCookieHeader(loginCookies));

        var revokeResponse = await client.SendAsync(revokeRequest);

        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
        var setCookies = GetSetCookieHeaders(revokeResponse);
        Assert.Contains(setCookies, x => x.StartsWith($"{AdminRefreshCookieName}=", StringComparison.Ordinal) && x.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(setCookies, x => x.StartsWith($"{AdminCsrfCookieName}=", StringComparison.Ordinal) && x.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Credentialed_cors_preflight_is_enabled_when_configured()
    {
        using var cookieFactory = CreateCookieModeFactory(allowLegacyBodyTokens: false);
        using var client = cookieFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/identity/auth/refresh");
        request.Headers.Add("Origin", "http://localhost:3001");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", $"{SurfaceHeaderName}, {CsrfHeaderName}, Content-Type");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(
            response.Headers.TryGetValues("Access-Control-Allow-Credentials", out var values),
            string.Join(Environment.NewLine, response.Headers.Select(x => $"{x.Key}: {string.Join(", ", x.Value)}")));
        Assert.Contains("true", values);
    }

    private WebApplicationFactory<Program> CreateCookieModeFactory(bool allowLegacyBodyTokens)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["BrowserSessions:TokenTransport"] = "RefreshCookie",
                    ["BrowserSessions:AllowLegacyBodyTokens"] = allowLegacyBodyTokens.ToString(),
                    ["BrowserSessions:CookieSecure"] = "false",
                    ["BrowserSessions:AdminRefreshCookieName"] = AdminRefreshCookieName,
                    ["BrowserSessions:AdminCsrfCookieName"] = AdminCsrfCookieName,
                    ["BrowserSessions:ClientRefreshCookieName"] = ClientRefreshCookieName,
                    ["BrowserSessions:ClientCsrfCookieName"] = ClientCsrfCookieName,
                    ["AppCors:AllowedOrigins:0"] = "http://localhost:3001",
                    ["AppCors:AllowCredentials"] = "true"
                });
            });
        });
    }

    private static async Task<HttpResponseMessage> LoginWithAdminCookieAsync(HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/auth/login")
        {
            Content = JsonContent.Create(new
            {
                email = "admin@test.local",
                password = "MyV3ryC00lAdminP@ss"
            })
        };
        request.Headers.Add(SurfaceHeaderName, "admin");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private static IReadOnlyList<string> GetSetCookieHeaders(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.ToArray()
            : [];
    }

    private static Dictionary<string, string> GetCookieJar(HttpResponseMessage response)
    {
        var cookies = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var header in GetSetCookieHeaders(response))
        {
            var pair = header.Split(';', 2)[0];
            var separatorIndex = pair.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                continue;
            }

            cookies[pair[..separatorIndex]] = pair[(separatorIndex + 1)..];
        }

        return cookies;
    }

    private static string BuildCookieHeader(IReadOnlyDictionary<string, string> cookies)
    {
        return string.Join("; ", cookies.Select(x => $"{x.Key}={x.Value}"));
    }

    private sealed class LoginEnvelope
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
    }

    private sealed class ClientLoginEnvelope
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
    }
}
