using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking;

public sealed class BookingModule : IModuleDefinition
{
    public string ModuleCode => "booking";

    public void ConfigurePersistence()
    {
    }

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
