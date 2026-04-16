using System.Reflection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Audit;
using Tailbook.Modules.Booking;
using Tailbook.Modules.Catalog;
using Tailbook.Modules.Customer;
using Tailbook.Modules.Identity;
using Tailbook.Modules.Notifications;
using Tailbook.Modules.Pets;
using Tailbook.Modules.Reporting;
using Tailbook.Modules.Staff;
using Tailbook.Modules.VisitOperations;

namespace Tailbook.Api.Host.Infrastructure;

public static class ModuleCatalog
{
    public static readonly Assembly[] EndpointAssemblies =
    [
        typeof(IdentityModule).Assembly,
        typeof(CustomerModule).Assembly,
        typeof(PetsModule).Assembly,
        typeof(CatalogModule).Assembly,
        typeof(BookingModule).Assembly,
        typeof(VisitOperationsModule).Assembly,
        typeof(StaffModule).Assembly,
        typeof(NotificationsModule).Assembly,
        typeof(AuditModule).Assembly,
        typeof(ReportingModule).Assembly
    ];

    public static IServiceCollection AddTailbookModules(this IServiceCollection services, IConfiguration configuration)
    {
        var modules = new IModuleDefinition[]
        {
            new IdentityModule(),
            new CustomerModule(),
            new PetsModule(),
            new CatalogModule(),
            new BookingModule(),
            new VisitOperationsModule(),
            new StaffModule(),
            new NotificationsModule(),
            new AuditModule(),
            new ReportingModule()
        };

        foreach (var module in modules) module.Register(services, configuration);

        services.AddSingleton<IReadOnlyCollection<IModuleDefinition>>(modules);
        return services;
    }

    public static WebApplication MapTailbookModules(this WebApplication app)
    {
        var modules = app.Services.GetRequiredService<IReadOnlyCollection<IModuleDefinition>>();

        foreach (var module in modules) module.MapEndpoints(app);

        return app;
    }
}
