using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Reporting.Infrastructure.Persistence.Configurations;
using Tailbook.Modules.Reporting.Infrastructure.Services;

namespace Tailbook.Modules.Reporting;

public sealed class ReportingModule : IModuleDefinition
{
    public string ModuleCode => "reporting";

    public void ConfigurePersistence()
    {
        ModelConfigurationRegistry.Register(ModuleCode, ReportingModelConfiguration.Apply);
    }

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ReportingQueries>();
        services.AddScoped<IReportingQueries>(sp => sp.GetRequiredService<ReportingQueries>());
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
