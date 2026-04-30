using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Audit.Application.AccessAuditEntries.Queries;
using Tailbook.Modules.Audit.Application.AuditEntries.Queries;
using Tailbook.Modules.Audit.Infrastructure.Persistence.Configurations;
using Tailbook.Modules.Audit.Infrastructure.Persistence.ReadModels;
using Tailbook.Modules.Audit.Infrastructure.Services;

namespace Tailbook.Modules.Audit;

public sealed class AuditModule : IModuleDefinition
{
    public string ModuleCode => "audit";

    public void ConfigurePersistence()
    {
        ModelConfigurationRegistry.Register(ModuleCode, AuditModelConfiguration.Apply);
    }

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAccessAuditEntryQueries, AccessAuditEntryReadService>();
        services.AddScoped<IAuditEntryQueries, AuditEntryReadService>();
        services.AddScoped<IAccessAuditService, AccessAuditService>();
        services.AddScoped<IAuditTrailService, AuditTrailService>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
