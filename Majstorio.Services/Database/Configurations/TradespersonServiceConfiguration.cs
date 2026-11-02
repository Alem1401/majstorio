using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majstorio.Services.Database.Configurations;

public class TradespersonServiceConfiguration : IEntityTypeConfiguration<TradespersonService>
{
    public void Configure(EntityTypeBuilder<TradespersonService> builder)
    {
        builder.Property(service => service.Name).HasMaxLength(ColumnLengths.Name).IsRequired();
        builder.Property(service => service.Description).HasMaxLength(ColumnLengths.LongText).IsRequired();
        builder.Property(service => service.MinimumPrice).HasPrecision(ColumnLengths.MoneyPrecision, ColumnLengths.MoneyScale);
        builder.Property(service => service.MaximumPrice).HasPrecision(ColumnLengths.MoneyPrecision, ColumnLengths.MoneyScale);

        // Advertised services have no life outside their profile, so they go with it.
        builder.HasOne(service => service.TradespersonProfile)
            .WithMany(profile => profile.TradespersonServices)
            .HasForeignKey(service => service.TradespersonProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(service => service.ServiceCategory)
            .WithMany(category => category.TradespersonServices)
            .HasForeignKey(service => service.ServiceCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
