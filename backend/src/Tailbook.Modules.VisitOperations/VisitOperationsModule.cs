using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.VisitOperations.Infrastructure.Persistence.Configurations;
using Tailbook.Modules.VisitOperations.Infrastructure.Services;

namespace Tailbook.Modules.VisitOperations;

public sealed class VisitOperationsModule : IModuleDefinition
{
    public string ModuleCode => "visitoperations";

    public void ConfigurePersistence()
    {
        ModelConfigurationRegistry.Register(ModuleCode, VisitOperationsModelConfiguration.Apply);
    }

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<VisitUseCases>();
        services.AddScoped<IVisitReadService>(sp => sp.GetRequiredService<VisitUseCases>());
        services.AddScoped<GroomerVisitUseCases>();
        services.AddScoped<IGroomerVisitReadService>(sp => sp.GetRequiredService<GroomerVisitUseCases>());
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
