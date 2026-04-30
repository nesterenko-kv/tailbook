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

namespace Tailbook.Api.Tests;

internal static class TestModelConfiguration
{
    private static readonly object Gate = new();
    private static bool configured;

    public static void Configure()
    {
        lock (Gate)
        {
            if (configured)
            {
                return;
            }

            new AuditModule().ConfigurePersistence();
            new BookingModule().ConfigurePersistence();
            new CatalogModule().ConfigurePersistence();
            new CustomerModule().ConfigurePersistence();
            new IdentityModule().ConfigurePersistence();
            new NotificationsModule().ConfigurePersistence();
            new PetsModule().ConfigurePersistence();
            new ReportingModule().ConfigurePersistence();
            new StaffModule().ConfigurePersistence();
            new VisitOperationsModule().ConfigurePersistence();
            configured = true;
        }
    }
}
