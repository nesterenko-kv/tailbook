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
        services.Configure<StaffSchedulingOptions>(configuration.GetSection(StaffSchedulingOptions.SectionName));
        services.AddSingleton<SalonTimeZoneProvider>();
        services.AddScoped<StaffQueries>();
        services.AddScoped<IStaffAccessPolicy, StaffAccessPolicy>();
        services.AddScoped<IStaffSchedulingService, StaffSchedulingService>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
