using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majstorio.Services.Database.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.Property(city => city.Name).HasMaxLength(ColumnLengths.Name).IsRequired();

        // Reference data names are mandatory and unique; used records are deactivated, not deleted.
        builder.HasIndex(city => city.Name).IsUnique();
    }
}
