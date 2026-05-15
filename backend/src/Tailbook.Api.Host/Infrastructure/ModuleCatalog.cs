using System.Reflection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
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
    private static readonly IModuleDefinition[] Modules =
    [
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
    ];

    public static readonly Assembly[] ModuleAssemblies = Modules.Select(x => x.GetType().Assembly).ToArray();
    public static readonly ModelConfigurationAssemblies PersistenceModelAssemblies = new(ModuleAssemblies);

    public static IServiceCollection AddTailbookModules(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(PersistenceModelAssemblies);

        foreach (var module in Modules)
        {
            module.Register(services, configuration);
        }

        services.AddSingleton<IReadOnlyCollection<IModuleDefinition>>(Modules);
        return services;
    }

    public static WebApplication MapTailbookModules(this WebApplication app)
    {
        var modules = app.Services.GetRequiredService<IReadOnlyCollection<IModuleDefinition>>();

        foreach (var module in modules)
        {
            module.MapEndpoints(app);
        }

        return app;
    }
}
