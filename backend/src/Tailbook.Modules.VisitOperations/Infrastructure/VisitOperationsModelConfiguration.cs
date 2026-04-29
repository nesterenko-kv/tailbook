using Microsoft.EntityFrameworkCore;
using Tailbook.Modules.VisitOperations.Domain;

namespace Tailbook.Modules.VisitOperations.Infrastructure;

public static class VisitOperationsModelConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Visit>(builder =>
        {
            builder.ToTable("visits", "visitops");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.HasIndex(x => x.AppointmentId).IsUnique();
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CheckedInAtUtc);
            builder.HasMany(x => x.ExecutionItems).WithOne().HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.ExecutionItems).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasMany(x => x.PriceAdjustments).WithOne().HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.PriceAdjustments).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<VisitExecutionItem>(builder =>
        {
            builder.ToTable("visit_execution_items", "visitops");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ItemType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.OfferCodeSnapshot).HasMaxLength(64).IsRequired();
            builder.Property(x => x.OfferDisplayNameSnapshot).HasMaxLength(200).IsRequired();
            builder.Property(x => x.PriceAmountSnapshot).HasPrecision(18, 2).IsRequired();
            builder.HasIndex(x => x.VisitId);
            builder.HasIndex(x => x.AppointmentItemId).IsUnique();
            builder.HasMany(x => x.PerformedProcedures).WithOne().HasForeignKey(x => x.VisitExecutionItemId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.PerformedProcedures).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasMany(x => x.SkippedComponents).WithOne().HasForeignKey(x => x.VisitExecutionItemId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.SkippedComponents).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<VisitPerformedProcedure>(builder =>
        {
            builder.ToTable("visit_performed_procedures", "visitops");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ProcedureCodeSnapshot).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ProcedureNameSnapshot).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Note).HasMaxLength(1000);
            builder.HasIndex(x => x.VisitExecutionItemId);
            builder.HasIndex(x => new { x.VisitExecutionItemId, x.ProcedureId }).IsUnique();
        });

        modelBuilder.Entity<VisitSkippedComponent>(builder =>
        {
            builder.ToTable("visit_skipped_components", "visitops");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ProcedureCodeSnapshot).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ProcedureNameSnapshot).HasMaxLength(200).IsRequired();
            builder.Property(x => x.OmissionReasonCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Note).HasMaxLength(1000);
            builder.HasIndex(x => x.VisitExecutionItemId);
            builder.HasIndex(x => new { x.VisitExecutionItemId, x.OfferVersionComponentId }).IsUnique();
        });

        modelBuilder.Entity<VisitPriceAdjustment>(builder =>
        {
            builder.ToTable("visit_price_adjustments", "visitops");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.ReasonCode).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Note).HasMaxLength(1000);
            builder.HasIndex(x => x.VisitId);
            builder.HasIndex(x => x.CreatedAtUtc);
        });
    }
}
