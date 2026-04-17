using Microsoft.EntityFrameworkCore;
using Tailbook.Modules.Reporting.Domain;

namespace Tailbook.Modules.Reporting.Infrastructure;

public static class ReportingModelConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReportingAppointment>(builder =>
        {
            builder.ToView("appointments", "booking");
            builder.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ReportingAppointmentItem>(builder =>
        {
            builder.ToView("appointment_items", "booking");
            builder.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ReportingPriceSnapshot>(builder =>
        {
            builder.ToView("price_snapshots", "booking");
            builder.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ReportingDurationSnapshot>(builder =>
        {
            builder.ToView("duration_snapshots", "booking");
            builder.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ReportingVisit>(builder =>
        {
            builder.ToView("visits", "visitops");
            builder.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ReportingVisitExecutionItem>(builder =>
        {
            builder.ToView("visit_execution_items", "visitops");
            builder.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ReportingVisitSkippedComponent>(builder =>
        {
            builder.ToView("visit_skipped_components", "visitops");
            builder.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ReportingVisitPriceAdjustment>(builder =>
        {
            builder.ToView("visit_price_adjustments", "visitops");
            builder.HasKey(x => x.Id);
        });
    }
}
