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
        services.AddScoped<BookingQuoteReadService>();
        services.AddScoped<BookingManagementUseCases>();
        services.AddScoped<IBookingManagementReadService>(sp => sp.GetRequiredService<BookingManagementUseCases>());
        services.AddScoped<ClientPortalBookingUseCases>();
        services.AddScoped<IClientPortalBookingReadService>(sp => sp.GetRequiredService<ClientPortalBookingUseCases>());
        services.AddScoped<PublicBookingReadService>();
        services.AddScoped<GroomerBookingReadService>();
        services.AddScoped<IGroomerBookingReadService>(sp => sp.GetRequiredService<GroomerBookingReadService>());
        services.AddScoped<IAppointmentOverlapReadService, BookingOverlapReadService>();
        services.AddScoped<IAppointmentVisitService, AppointmentVisitService>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
