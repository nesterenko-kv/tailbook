using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tailbook.Modules.Reporting.Infrastructure.Persistence.Configurations;

public sealed class ReportingAppointmentItemConfiguration : IEntityTypeConfiguration<ReportingAppointmentItem>
{
    public void Configure(EntityTypeBuilder<ReportingAppointmentItem> builder)
    {
        builder.ToView("appointment_items", "booking");
            builder.HasKey(x => x.Id);
    }
}
