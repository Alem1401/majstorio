using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majstorio.Services.Database.Configurations;

public class TradespersonProfileConfiguration : IEntityTypeConfiguration<TradespersonProfile>
{
    public void Configure(EntityTypeBuilder<TradespersonProfile> builder)
    {
        builder.Property(profile => profile.Description).HasMaxLength(ColumnLengths.LongText).IsRequired();

        // One-to-one: the foreign key on this side gets a unique index.
        builder.HasOne(profile => profile.User)
            .WithOne(user => user.TradespersonProfile)
            .HasForeignKey<TradespersonProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(profile => profile.City)
            .WithMany(city => city.TradespersonProfiles)
            .HasForeignKey(profile => profile.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(profile => profile.ApprovedByUser)
            .WithMany(user => user.ApprovedTradespersonProfiles)
            .HasForeignKey(profile => profile.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
