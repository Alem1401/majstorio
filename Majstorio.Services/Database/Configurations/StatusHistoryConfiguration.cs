using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majstorio.Services.Database.Configurations;

public class StatusHistoryConfiguration : IEntityTypeConfiguration<StatusHistory>
{
    public void Configure(EntityTypeBuilder<StatusHistory> builder)
    {
        builder.Property(history => history.Note).HasMaxLength(ColumnLengths.LongText);

        // History rows belong to their request and go with it.
        builder.HasOne(history => history.ServiceRequest)
            .WithMany(request => request.StatusHistories)
            .HasForeignKey(history => history.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(history => history.Appointment)
            .WithMany(appointment => appointment.StatusHistories)
            .HasForeignKey(history => history.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(history => history.ChangedByUser)
            .WithMany(user => user.StatusHistories)
            .HasForeignKey(history => history.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
