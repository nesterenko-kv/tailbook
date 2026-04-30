using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking.Infrastructure.Persistence.Configurations;
using Tailbook.Modules.Booking.Infrastructure.Services;

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
        services.AddScoped<BookingSnapshotComposer>();
        services.AddScoped<IBookingSnapshotComposer>(sp => sp.GetRequiredService<BookingSnapshotComposer>());
        services.AddScoped<BookingQuoteQueries>();
        services.AddScoped<BookingManagementQueries>();
        services.AddScoped<IBookingManagementQueries>(sp => sp.GetRequiredService<BookingManagementQueries>());
        services.AddScoped<ClientPortalBookingQueries>();
        services.AddScoped<IClientPortalBookingQueries>(sp => sp.GetRequiredService<ClientPortalBookingQueries>());
        services.AddScoped<PublicBookingQueries>();
        services.AddScoped<GroomerBookingQueries>();
        services.AddScoped<IGroomerBookingQueries>(sp => sp.GetRequiredService<GroomerBookingQueries>());
        services.AddScoped<IAppointmentOverlapReadService, BookingOverlapReadService>();
        services.AddScoped<IAppointmentVisitService, AppointmentVisitService>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
