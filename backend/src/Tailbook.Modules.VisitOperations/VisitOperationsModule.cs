using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.VisitOperations.Infrastructure.BackgroundJobs;
using Tailbook.Modules.VisitOperations.Infrastructure.Services;
using Tailbook.Modules.VisitOperations.Infrastructure.Services.CommandHandlers;

namespace Tailbook.Modules.VisitOperations;

public sealed class VisitOperationsModule : IModuleDefinition
{
    public string ModuleCode => "visitoperations";

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IVisitReadService, VisitReadService>();
        services.AddScoped<IGroomerVisitReadService, GroomerVisitReadService>();
        services.AddScoped<CheckInAppointmentUseCaseCommandHandler>();
        services.AddScoped<CheckInOwnAppointmentUseCaseCommandHandler>();
        services.AddScoped<RecordPerformedProcedureUseCaseCommandHandler>();
        services.AddScoped<RecordSkippedComponentUseCaseCommandHandler>();
        services.AddHostedService<VisitCancellationConsumer>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
