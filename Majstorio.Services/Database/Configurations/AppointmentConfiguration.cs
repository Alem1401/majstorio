using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majstorio.Services.Database.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        // One-to-one: an offer has at most one appointment.
        builder.HasOne(appointment => appointment.Offer)
            .WithOne(offer => offer.Appointment)
            .HasForeignKey<Appointment>(appointment => appointment.OfferId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
