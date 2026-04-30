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
        services.AddScoped<VisitQueries>();
        services.AddScoped<IVisitQueries>(sp => sp.GetRequiredService<VisitQueries>());
        services.AddScoped<GroomerVisitQueries>();
        services.AddScoped<IGroomerVisitQueries>(sp => sp.GetRequiredService<GroomerVisitQueries>());
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
