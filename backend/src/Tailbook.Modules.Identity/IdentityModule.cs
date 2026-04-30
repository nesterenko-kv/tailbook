using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Infrastructure.Options;
using Tailbook.Modules.Identity.Infrastructure.Persistence.Configurations;
using Tailbook.Modules.Identity.Infrastructure.Seeding;
using Tailbook.Modules.Identity.Infrastructure.Services;

namespace Tailbook.Modules.Identity;

public sealed class IdentityModule : IModuleDefinition
{
    public string ModuleCode => "identity";

    public void ConfigurePersistence()
    {
        ModelConfigurationRegistry.Register(ModuleCode, IdentityModelConfiguration.Apply);
    }

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
        services.AddOptions<PasswordResetOptions>()
            .Bind(configuration.GetSection(PasswordResetOptions.SectionName))
            .Validate(x => x.ExpirationMinutes > 0, "PasswordReset:ExpirationMinutes must be greater than zero.")
            .Validate(x => x.TokenBytes >= 32, "PasswordReset:TokenBytes must be at least 32.")
            .ValidateOnStart();
        services.AddScoped<JwtTokenFactory>();
        services.AddScoped<PasswordHasher>();
        services.AddSingleton<LoginThrottlingService>();
        services.AddSingleton<ILoginThrottlingService>(sp => sp.GetRequiredService<LoginThrottlingService>());
        services.AddScoped<RefreshTokenService>();
        services.AddScoped<PasswordResetService>();
        services.AddScoped<IPasswordResetService>(sp => sp.GetRequiredService<PasswordResetService>());
        services.AddScoped<MfaFactorService>();
        services.AddScoped<IMfaFactorService>(sp => sp.GetRequiredService<MfaFactorService>());
        services.AddScoped<IdentitySessionService>();
        services.AddScoped<IIdentitySessionService>(sp => sp.GetRequiredService<IdentitySessionService>());
        services.AddScoped<IdentityQueries>();
        services.AddScoped<IIdentityQueries>(sp => sp.GetRequiredService<IdentityQueries>());
        services.AddScoped<ClientPortalIdentityQueries>();
        services.AddScoped<IClientPortalIdentityQueries>(sp => sp.GetRequiredService<ClientPortalIdentityQueries>());
        services.AddScoped<AuthenticateUserCommandHandler>();
        services.AddScoped<IAuthenticateUserService>(sp => sp.GetRequiredService<AuthenticateUserCommandHandler>());
        services.AddScoped<RegisterClientPortalUserCommandHandler>();
        services.AddScoped<IRegisterClientPortalUserHandler>(sp => sp.GetRequiredService<RegisterClientPortalUserCommandHandler>());
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
