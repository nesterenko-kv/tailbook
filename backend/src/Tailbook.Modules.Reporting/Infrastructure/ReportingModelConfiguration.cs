using Microsoft.EntityFrameworkCore;
using Tailbook.Modules.Reporting.Domain;

namespace Tailbook.Modules.Reporting.Infrastructure;

public static class ReportingModelConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReportingAppointment>(builder =>
        {
            builder.ToTable("appointments", "booking");
            builder.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ReportingAppointmentItem>(builder =>
        {
            builder.ToTable("appointment_items", "booking");
            builder.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ReportingPriceSnapshot>(builder =>
        {
            builder.ToTable("price_snapshots", "booking");
            builder.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ReportingDurationSnapshot>(builder =>
        {
            builder.ToTable("duration_snapshots", "booking");
            builder.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ReportingVisit>(builder =>
        {
            builder.ToTable("visits", "visitops");
            builder.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ReportingVisitExecutionItem>(builder =>
        {
            builder.ToTable("visit_execution_items", "visitops");
            builder.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ReportingVisitSkippedComponent>(builder =>
        {
            builder.ToTable("visit_skipped_components", "visitops");
            builder.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ReportingVisitPriceAdjustment>(builder =>
        {
            builder.ToTable("visit_price_adjustments", "visitops");
            builder.HasKey(x => x.Id);
        });
    }
}
