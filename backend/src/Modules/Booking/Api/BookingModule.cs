using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Booking.Infrastructure.Services;
using Tailbook.Modules.Booking.Infrastructure.Services.CommandHandlers;

namespace Tailbook.Modules.Booking;

public sealed class BookingModule : IModuleDefinition
{
    public string ModuleCode => "booking";

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<BookingSnapshotComposer>();
        services.AddScoped<IBookingSnapshotComposer, BookingSnapshotComposer>();
        services.AddScoped<BookingQuoteReadService>();
        services.AddScoped<IBookingManagementReadService, BookingManagementReadService>();
        services.AddScoped<IClientPortalBookingReadService, ClientPortalBookingReadService>();
        services.AddScoped<CreateBookingRequestUseCaseCommandHandler>();
        services.AddScoped<CreateAppointmentUseCaseCommandHandler>();
        services.AddScoped<PublicBookingReadService>();
        services.AddScoped<GroomerBookingReadService>();
        services.AddScoped<IGroomerBookingReadService, GroomerBookingReadService>();
        services.AddScoped<IAppointmentOverlapReadService, BookingOverlapReadService>();
        services.AddScoped<IAppointmentVisitService, AppointmentVisitService>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
