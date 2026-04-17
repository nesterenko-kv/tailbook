using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Staff.Application;
using Tailbook.Modules.Staff.Infrastructure;

namespace Tailbook.Modules.Staff;

public sealed class StaffModule : IModuleDefinition
{
    public string ModuleCode => "staff";

    public void ConfigurePersistence()
    {
        ModelConfigurationRegistry.Register(ModuleCode, StaffModelConfiguration.Apply);
    }

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StaffSchedulingOptions>()
            .Bind(configuration.GetSection(StaffSchedulingOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.TimeZoneId), "StaffScheduling:TimeZoneId is required.")
            .ValidateOnStart();
        services.AddSingleton<SalonTimeZoneProvider>();
        services.AddScoped<StaffQueries>();
        services.AddScoped<IStaffAccessPolicy, StaffAccessPolicy>();
        services.AddScoped<IStaffSchedulingService, StaffSchedulingService>();
        services.AddScoped<IGroomerProfileReadService, GroomerProfileReadService>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
