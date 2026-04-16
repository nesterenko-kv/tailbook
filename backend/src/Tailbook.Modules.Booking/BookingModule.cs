using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking.Application;
using Tailbook.Modules.Booking.Infrastructure;

namespace Tailbook.Modules.Booking;

public sealed class BookingModule : IModuleDefinition
{
    public string ModuleCode => "booking";

    public void ConfigurePersistence()
    {
        ModelConfigurationRegistry.Register(ModuleCode, BookingModelConfiguration.Apply);
    }

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IBookingAccessPolicy, BookingAccessPolicy>();
        services.AddScoped<BookingQuoteQueries>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
