using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majstorio.Services.Database.Configurations;

public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.Property(category => category.Name).HasMaxLength(ColumnLengths.Name).IsRequired();

        builder.HasIndex(category => category.Name).IsUnique();
    }
}
