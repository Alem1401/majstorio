using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majstorio.Services.Database.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(notification => notification.Title).HasMaxLength(ColumnLengths.ShortText).IsRequired();
        builder.Property(notification => notification.Message).HasMaxLength(ColumnLengths.LongText).IsRequired();

        builder.HasOne(notification => notification.User)
            .WithMany(user => user.Notifications)
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Notifications about a request are part of that request's trail.
        builder.HasOne(notification => notification.ServiceRequest)
            .WithMany(request => request.Notifications)
            .HasForeignKey(notification => notification.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // The worker may receive the same message twice; the second insert must fail.
        builder.HasIndex(notification => notification.EventId).IsUnique();
    }
}
