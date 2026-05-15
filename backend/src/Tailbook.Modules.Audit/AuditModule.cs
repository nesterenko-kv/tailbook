using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Audit.Application.AccessAuditEntries.Queries;
using Tailbook.Modules.Audit.Application.AuditEntries.Queries;
using Tailbook.Modules.Audit.Infrastructure.Persistence.ReadModels;
using Tailbook.Modules.Audit.Infrastructure.Services;
using Tailbook.Modules.Audit.Infrastructure.WriteBuffering;

namespace Tailbook.Modules.Audit;

public sealed class AuditModule : IModuleDefinition
{
    public string ModuleCode => "audit";

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<AuditWriteOptions>()
            .Bind(configuration.GetSection(AuditWriteOptions.SectionName))
            .Validate(x => x.QueueCapacity > 0, "Audit:QueueCapacity must be greater than zero.")
            .Validate(x => x.BatchSize > 0, "Audit:BatchSize must be greater than zero.")
            .Validate(x => x.BatchSize <= x.QueueCapacity, "Audit:BatchSize must be less than or equal to Audit:QueueCapacity.")
            .Validate(x => x.FlushIntervalMilliseconds > 0, "Audit:FlushIntervalMilliseconds must be greater than zero.")
            .Validate(x => x.MaxWriteRetries >= 0, "Audit:MaxWriteRetries must be zero or greater.")
            .Validate(x => x.RetryDelayMilliseconds > 0, "Audit:RetryDelayMilliseconds must be greater than zero.")
            .ValidateOnStart();

        services.AddScoped<IAccessAuditEntryReadService, AccessAuditEntryReadService>();
        services.AddScoped<IAuditEntryReadService, AuditEntryReadService>();
        services.AddSingleton<IAuditWriteQueue, AuditWriteQueue>();
        services.AddHostedService<AuditBatchWriterHostedService>();
        services.AddScoped<IAccessAuditService, AccessAuditService>();
        services.AddScoped<IAuditTrailService, AuditTrailService>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
