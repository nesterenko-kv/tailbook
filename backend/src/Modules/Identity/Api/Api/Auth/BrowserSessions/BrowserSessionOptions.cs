namespace Tailbook.Modules.Identity.Api.Auth.BrowserSessions;

public sealed class BrowserSessionOptions
{
    public const string SectionName = "BrowserSessions";

    public string TokenTransport { get; set; } = BrowserSessionTokenTransportModes.BodyTokens;
    public bool AllowLegacyBodyTokens { get; set; } = true;
    public string SurfaceHeaderName { get; set; } = "X-Tailbook-Session-Surface";
    public string CsrfHeaderName { get; set; } = "X-Tailbook-CSRF";
    public bool CookieSecure { get; set; } = true;
    public string CookieSameSite { get; set; } = "Lax";
    public string AdminRefreshCookieName { get; set; } = "__Host-tailbook-admin-refresh";
    public string GroomerRefreshCookieName { get; set; } = "__Host-tailbook-groomer-refresh";
    public string ClientRefreshCookieName { get; set; } = "__Host-tailbook-client-refresh";
    public string AdminCsrfCookieName { get; set; } = "__Host-tailbook-admin-csrf";
    public string GroomerCsrfCookieName { get; set; } = "__Host-tailbook-groomer-csrf";
    public string ClientCsrfCookieName { get; set; } = "__Host-tailbook-client-csrf";

    public bool UseRefreshCookies =>
        string.Equals(TokenTransport, BrowserSessionTokenTransportModes.RefreshCookie, StringComparison.OrdinalIgnoreCase);

    public static bool HasValidTokenTransport(BrowserSessionOptions options)
    {
        return string.Equals(options.TokenTransport, BrowserSessionTokenTransportModes.BodyTokens, StringComparison.OrdinalIgnoreCase)
               || string.Equals(options.TokenTransport, BrowserSessionTokenTransportModes.RefreshCookie, StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasValidSameSite(BrowserSessionOptions options)
    {
        return string.Equals(options.CookieSameSite, "Strict", StringComparison.OrdinalIgnoreCase)
               || string.Equals(options.CookieSameSite, "Lax", StringComparison.OrdinalIgnoreCase)
               || string.Equals(options.CookieSameSite, "None", StringComparison.OrdinalIgnoreCase);
    }
}

public static class BrowserSessionTokenTransportModes
{
    public const string BodyTokens = "BodyTokens";
    public const string RefreshCookie = "RefreshCookie";
}

public static class BrowserSessionSurfaces
{
    public const string Admin = "admin";
    public const string Groomer = "groomer";
    public const string Client = "client";
}
