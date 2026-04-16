#nullable disable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Api.Host.Migrations;

[DbContext(typeof(AppDbContext))]
internal class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasDefaultSchema("public")
            .HasAnnotation("ProductVersion", "10.0.6")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.UseIdentityByDefaultColumns();

        modelBuilder.Entity("Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration.OutboxMessage", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid");

            b.Property<string>("EventType")
                .IsRequired()
                .HasMaxLength(512)
                .HasColumnType("character varying(512)");

            b.Property<string>("ModuleCode")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            b.Property<DateTime>("OccurredAtUtc")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("PayloadJson")
                .IsRequired()
                .HasColumnType("jsonb");

            b.Property<DateTime?>("ProcessedAtUtc")
                .HasColumnType("timestamp with time zone");

            b.HasKey("Id");

            b.HasIndex("ModuleCode", "OccurredAtUtc");

            b.HasIndex("ProcessedAtUtc");

            b.ToTable("outbox_messages", "integration");
        });
#pragma warning restore 612, 618
    }
}
