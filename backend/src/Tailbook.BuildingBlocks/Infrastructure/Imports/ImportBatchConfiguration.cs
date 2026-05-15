using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.BuildingBlocks.Infrastructure.Imports;

public sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("import_batches", "integration");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Domain).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SourceName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.TotalRows).IsRequired();
        builder.Property(x => x.ValidRows).IsRequired();
        builder.Property(x => x.ErrorRows).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.Domain, x.CreatedAt });
        builder.HasMany(x => x.Rows).WithOne().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Issues).WithOne().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Rows).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Issues).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ImportBatchRowConfiguration : IEntityTypeConfiguration<ImportBatchRow>
{
    public void Configure(EntityTypeBuilder<ImportBatchRow> builder)
    {
        builder.ToTable("import_batch_rows", "integration");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowNumber).IsRequired();
        builder.Property(x => x.ExternalId).HasMaxLength(128);
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.HasIndex(x => new { x.BatchId, x.RowNumber }).IsUnique();
        builder.HasIndex(x => new { x.BatchId, x.ExternalId });
    }
}

public sealed class ImportBatchIssueConfiguration : IEntityTypeConfiguration<ImportBatchIssueEntity>
{
    public void Configure(EntityTypeBuilder<ImportBatchIssueEntity> builder)
    {
        builder.ToTable("import_batch_issues", "integration");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowNumber).IsRequired();
        builder.Property(x => x.Field).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => new { x.BatchId, x.RowNumber });
    }
}
