using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Reporting.Application;
using Tailbook.Modules.Reporting.Infrastructure;

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
        services.AddScoped<IReportingAccessPolicy, ReportingAccessPolicy>();
        services.AddScoped<ReportingQueries>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
