using System.Security.Cryptography;
using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Tailbook.Modules.Identity.Api.Auth.BrowserSessions;

public sealed class BrowserSessionService(IOptions<BrowserSessionOptions> optionsAccessor)
{
    private static readonly string[] IdentitySurfaces = [BrowserSessionSurfaces.Admin, BrowserSessionSurfaces.Groomer];
    private static readonly string[] ClientSurfaces = [BrowserSessionSurfaces.Client];

    private readonly BrowserSessionOptions _options = optionsAccessor.Value;

    public ErrorOr<BrowserSessionResponseMode> ApplyIdentitySession(HttpContext httpContext, LoginResult login)
    {
        return ApplySession(httpContext, login.RefreshToken, login.RefreshTokenExpiresAt, defaultSurface: null, IdentitySurfaces);
    }

    public ErrorOr<BrowserSessionResponseMode> ApplyClientSession(HttpContext httpContext, LoginResult login)
    {
        return ApplySession(httpContext, login.RefreshToken, login.RefreshTokenExpiresAt, BrowserSessionSurfaces.Client, ClientSurfaces);
    }

    public ErrorOr<BrowserRefreshTokenResolution> ResolveIdentityRefreshToken(HttpContext httpContext, string? requestRefreshToken)
    {
        return ResolveRefreshToken(httpContext, requestRefreshToken, defaultSurface: null, IdentitySurfaces);
    }

    public ErrorOr<BrowserRefreshTokenResolution> ResolveClientRefreshToken(HttpContext httpContext, string? requestRefreshToken)
    {
        return ResolveRefreshToken(httpContext, requestRefreshToken, BrowserSessionSurfaces.Client, ClientSurfaces);
    }

    public void ClearRefreshCookie(HttpContext httpContext, string surface)
    {
        if (!_options.UseRefreshCookies)
        {
            return;
        }

        var refreshCookieName = GetRefreshCookieName(surface);
        var csrfCookieName = GetCsrfCookieName(surface);
        var cookieOptions = BuildCookieOptions(DateTimeOffset.UnixEpoch, httpOnly: true);
        var csrfCookieOptions = BuildCookieOptions(DateTimeOffset.UnixEpoch, httpOnly: false);

        httpContext.Response.Cookies.Delete(refreshCookieName, cookieOptions);
        httpContext.Response.Cookies.Delete(csrfCookieName, csrfCookieOptions);
    }

    private ErrorOr<BrowserSessionResponseMode> ApplySession(
        HttpContext httpContext,
        string refreshToken,
        DateTimeOffset refreshTokenExpiresAt,
        string? defaultSurface,
        IReadOnlyCollection<string> allowedSurfaces)
    {
        if (!_options.UseRefreshCookies)
        {
            return new BrowserSessionResponseMode(IncludeRefreshTokenInResponse: true);
        }

        var surfaceResult = ResolveSurface(httpContext, defaultSurface, allowedSurfaces);
        if (surfaceResult.IsError)
        {
            return _options.AllowLegacyBodyTokens
                ? new BrowserSessionResponseMode(IncludeRefreshTokenInResponse: true)
                : surfaceResult.Errors;
        }

        var surface = surfaceResult.Value;
        httpContext.Response.Cookies.Append(
            GetRefreshCookieName(surface),
            refreshToken,
            BuildCookieOptions(refreshTokenExpiresAt, httpOnly: true));
        httpContext.Response.Cookies.Append(
            GetCsrfCookieName(surface),
            CreateCsrfToken(),
            BuildCookieOptions(refreshTokenExpiresAt, httpOnly: false));

        return new BrowserSessionResponseMode(IncludeRefreshTokenInResponse: false);
    }

    private ErrorOr<BrowserRefreshTokenResolution> ResolveRefreshToken(
        HttpContext httpContext,
        string? requestRefreshToken,
        string? defaultSurface,
        IReadOnlyCollection<string> allowedSurfaces)
    {
        if (!_options.UseRefreshCookies)
        {
            return string.IsNullOrWhiteSpace(requestRefreshToken)
                ? IdentityErrors.InvalidRefreshToken()
                : new BrowserRefreshTokenResolution(requestRefreshToken, Surface: null, Source: BrowserRefreshTokenSource.RequestBody);
        }

        var surfaceResult = ResolveSurface(httpContext, defaultSurface, allowedSurfaces);
        if (!surfaceResult.IsError)
        {
            var surface = surfaceResult.Value;
            var refreshCookieName = GetRefreshCookieName(surface);
            if (httpContext.Request.Cookies.TryGetValue(refreshCookieName, out var cookieRefreshToken)
                && !string.IsNullOrWhiteSpace(cookieRefreshToken))
            {
                var csrfResult = ValidateCsrf(httpContext, surface);
                if (csrfResult.IsError)
                {
                    return csrfResult.Errors;
                }

                return new BrowserRefreshTokenResolution(cookieRefreshToken, surface, BrowserRefreshTokenSource.Cookie);
            }
        }

        if (_options.AllowLegacyBodyTokens && !string.IsNullOrWhiteSpace(requestRefreshToken))
        {
            return new BrowserRefreshTokenResolution(requestRefreshToken, Surface: null, Source: BrowserRefreshTokenSource.RequestBody);
        }

        if (surfaceResult.IsError)
        {
            return surfaceResult.Errors;
        }

        return IdentityErrors.InvalidRefreshToken();
    }

    private ErrorOr<string> ResolveSurface(
        HttpContext httpContext,
        string? defaultSurface,
        IReadOnlyCollection<string> allowedSurfaces)
    {
        var surface = httpContext.Request.Headers[_options.SurfaceHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(surface))
        {
            surface = defaultSurface ?? string.Empty;
        }

        surface = surface.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(surface))
        {
            return IdentityErrors.BrowserSessionSurfaceRequired();
        }

        return allowedSurfaces.Contains(surface, StringComparer.OrdinalIgnoreCase)
            ? surface
            : IdentityErrors.InvalidBrowserSessionSurface();
    }

    private ErrorOr<Success> ValidateCsrf(HttpContext httpContext, string surface)
    {
        var headerToken = httpContext.Request.Headers[_options.CsrfHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(headerToken))
        {
            return IdentityErrors.InvalidBrowserSessionCsrf();
        }

        return httpContext.Request.Cookies.TryGetValue(GetCsrfCookieName(surface), out var cookieToken)
               && CryptographicOperations.FixedTimeEquals(
                   System.Text.Encoding.UTF8.GetBytes(headerToken),
                   System.Text.Encoding.UTF8.GetBytes(cookieToken))
            ? Result.Success
            : IdentityErrors.InvalidBrowserSessionCsrf();
    }

    private CookieOptions BuildCookieOptions(DateTimeOffset expiresAt, bool httpOnly)
    {
        return new CookieOptions
        {
            Expires = expiresAt,
            HttpOnly = httpOnly,
            IsEssential = true,
            Path = "/",
            SameSite = ParseSameSiteMode(_options.CookieSameSite),
            Secure = _options.CookieSecure
        };
    }

    private string GetRefreshCookieName(string surface)
    {
        return surface switch
        {
            BrowserSessionSurfaces.Admin => _options.AdminRefreshCookieName,
            BrowserSessionSurfaces.Groomer => _options.GroomerRefreshCookieName,
            BrowserSessionSurfaces.Client => _options.ClientRefreshCookieName,
            _ => throw new InvalidOperationException("Unsupported browser session surface.")
        };
    }

    private string GetCsrfCookieName(string surface)
    {
        return surface switch
        {
            BrowserSessionSurfaces.Admin => _options.AdminCsrfCookieName,
            BrowserSessionSurfaces.Groomer => _options.GroomerCsrfCookieName,
            BrowserSessionSurfaces.Client => _options.ClientCsrfCookieName,
            _ => throw new InvalidOperationException("Unsupported browser session surface.")
        };
    }

    private static SameSiteMode ParseSameSiteMode(string value)
    {
        if (string.Equals(value, "Strict", StringComparison.OrdinalIgnoreCase))
        {
            return SameSiteMode.Strict;
        }

        if (string.Equals(value, "None", StringComparison.OrdinalIgnoreCase))
        {
            return SameSiteMode.None;
        }

        return SameSiteMode.Lax;
    }

    private static string CreateCsrfToken()
    {
        return WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }
}

public sealed record BrowserSessionResponseMode(bool IncludeRefreshTokenInResponse);

public sealed record BrowserRefreshTokenResolution(
    string RefreshToken,
    string? Surface,
    BrowserRefreshTokenSource Source);

public enum BrowserRefreshTokenSource
{
    RequestBody,
    Cookie
}
