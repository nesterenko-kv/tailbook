using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Reporting.Infrastructure.Persistence.Configurations;

public sealed class ReportingAppointmentConfiguration : IEntityTypeConfiguration<ReportingAppointment>
{
    public void Configure(EntityTypeBuilder<ReportingAppointment> builder)
    {
        builder.ToView("appointments", "booking");
            builder.HasKey(x => x.Id);
    }
}
