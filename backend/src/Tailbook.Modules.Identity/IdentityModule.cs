using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Api.Auth.BrowserSessions;
using Tailbook.Modules.Identity.Infrastructure.Options;
using Tailbook.Modules.Identity.Infrastructure.Seeding;
using Tailbook.Modules.Identity.Infrastructure.Services;

namespace Tailbook.Modules.Identity;

public sealed class IdentityModule : IModuleDefinition
{
    public string ModuleCode => "identity";

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BootstrapAdminOptions>()
            .Bind(configuration.GetSection(BootstrapAdminOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.Email), "BootstrapAdmin:Email is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Password) && x.Password.Length >= 12, "BootstrapAdmin:Password must be at least 12 characters long.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.DisplayName), "BootstrapAdmin:DisplayName is required.")
            .ValidateOnStart();
        services.AddOptions<LoginThrottlingOptions>()
            .Bind(configuration.GetSection(LoginThrottlingOptions.SectionName))
            .Validate(x => x.MaxFailedAttempts > 0, "LoginThrottling:MaxFailedAttempts must be greater than zero.")
            .Validate(x => x.FailureWindowMinutes > 0, "LoginThrottling:FailureWindowMinutes must be greater than zero.")
            .Validate(x => x.LockoutMinutes > 0, "LoginThrottling:LockoutMinutes must be greater than zero.")
            .ValidateOnStart();
        services.AddOptions<RefreshTokenOptions>()
            .Bind(configuration.GetSection(RefreshTokenOptions.SectionName))
            .Validate(x => x.ExpirationDays > 0, "RefreshTokens:ExpirationDays must be greater than zero.")
            .Validate(x => x.TokenBytes >= 32, "RefreshTokens:TokenBytes must be at least 32.")
            .ValidateOnStart();
        services.AddOptions<BrowserSessionOptions>()
            .Bind(configuration.GetSection(BrowserSessionOptions.SectionName))
            .Validate(BrowserSessionOptions.HasValidTokenTransport, "BrowserSessions:TokenTransport must be BodyTokens or RefreshCookie.")
            .Validate(BrowserSessionOptions.HasValidSameSite, "BrowserSessions:CookieSameSite must be Strict, Lax, or None.")
            .ValidateOnStart();
        services.AddOptions<MfaChallengeOptions>()
            .Bind(configuration.GetSection(MfaChallengeOptions.SectionName))
            .Validate(x => x.ExpirationMinutes is >= 1 and <= 30, "MfaChallenge:ExpirationMinutes must be between 1 and 30.")
            .Validate(x => x.CodeLength is >= 6 and <= 10, "MfaChallenge:CodeLength must be between 6 and 10.")
            .Validate(x => x.MaxFailedAttempts is >= 1 and <= 10, "MfaChallenge:MaxFailedAttempts must be between 1 and 10.")
            .ValidateOnStart();
        services.AddOptions<MfaRecoveryCodeOptions>()
            .Bind(configuration.GetSection(MfaRecoveryCodeOptions.SectionName))
            .Validate(x => x.CodeCount is >= 4 and <= 20, "MfaRecoveryCodes:CodeCount must be between 4 and 20.")
            .Validate(x => x.CodeLength is >= 12 and <= 32, "MfaRecoveryCodes:CodeLength must be between 12 and 32.")
            .ValidateOnStart();
        services.AddOptions<PasswordResetOptions>()
            .Bind(configuration.GetSection(PasswordResetOptions.SectionName))
            .Validate(x => x.ExpirationMinutes > 0, "PasswordReset:ExpirationMinutes must be greater than zero.")
            .Validate(x => x.TokenBytes >= 32, "PasswordReset:TokenBytes must be at least 32.")
            .Validate(x => Uri.TryCreate(x.ResetUrlBase, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps), "PasswordReset:ResetUrlBase must be a valid absolute HTTP/HTTPS URL.")
            .ValidateOnStart();
        services.AddOptions<DeviceTrustOptions>()
            .Bind(configuration.GetSection(DeviceTrustOptions.SectionName))
            .Validate(x => x.DurationDays > 0, "DeviceTrust:DurationDays must be greater than zero.")
            .Validate(x => x.TokenBytes >= 16, "DeviceTrust:TokenBytes must be at least 16.")
            .ValidateOnStart();
        services.AddScoped<JwtTokenFactory>();
        services.AddScoped<PasswordHasher>();
        services.AddSingleton<ILoginThrottlingService, LoginThrottlingService>();
        services.AddScoped<RefreshTokenService>();
        services.AddScoped<BrowserSessionService>();
        services.AddScoped<PasswordResetService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<MfaFactorService>();
        services.AddScoped<IMfaFactorService, MfaFactorService>();
        services.AddScoped<MfaChallengeService>();
        services.AddScoped<IMfaChallengeService, MfaChallengeService>();
        services.AddScoped<MfaRecoveryCodeService>();
        services.AddScoped<IMfaRecoveryCodeService, MfaRecoveryCodeService>();
        services.AddScoped<IdentitySessionService>();
        services.AddScoped<IIdentitySessionService, IdentitySessionService>();
        services.AddScoped<IdentityUseCases>();
        services.AddScoped<IIdentityReadService, IdentityUseCases>();
        services.AddScoped<AuthenticateUserCommandHandler>();
        services.AddScoped<IAuthenticateUserService, AuthenticateUserCommandHandler>();
        services.AddScoped<DeviceTrustService>();
        services.AddScoped<IDeviceTrustService, DeviceTrustService>();
        services.AddScoped<IScopeAuthorizationService, ScopeAuthorizationService>();
        services.AddScoped<RegisterClientPortalUserHandler>();
        services.AddScoped<IRegisterClientPortalUserHandler, RegisterClientPortalUserHandler>();
        services.AddScoped<IUserReferenceValidationService, IdentityReferenceServices>();
        services.AddScoped<IClientPortalActorService, IdentityReferenceServices>();
        services.AddScoped<IDataSeeder, IdentitySeeder>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
