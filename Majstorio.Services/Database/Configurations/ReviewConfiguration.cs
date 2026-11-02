using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majstorio.Services.Database.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.Property(review => review.Comment).HasMaxLength(ColumnLengths.LongText);

        // One-to-one: an appointment has zero or one review.
        builder.HasOne(review => review.Appointment)
            .WithOne(appointment => appointment.Review)
            .HasForeignKey<Review>(review => review.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(review => review.TradespersonProfile)
            .WithMany(profile => profile.Reviews)
            .HasForeignKey(review => review.TradespersonProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(review => review.Customer)
            .WithMany(user => user.Reviews)
            .HasForeignKey(review => review.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
