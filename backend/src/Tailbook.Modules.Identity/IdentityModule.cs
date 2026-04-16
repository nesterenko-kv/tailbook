using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Application;
using Tailbook.Modules.Identity.Infrastructure;

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
        services.Configure<BootstrapAdminOptions>(configuration.GetSection(BootstrapAdminOptions.SectionName));
        services.AddScoped<JwtTokenFactory>();
        services.AddScoped<PasswordHasher>();
        services.AddScoped<IdentityQueries>();
        services.AddScoped<IIdentityAccessPolicy, IdentityAccessPolicy>();
        services.AddScoped<IDataSeeder, IdentitySeeder>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
