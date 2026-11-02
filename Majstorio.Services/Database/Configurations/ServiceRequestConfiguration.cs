using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majstorio.Services.Database.Configurations;

public class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.Property(request => request.Description).HasMaxLength(ColumnLengths.LongText).IsRequired();
        builder.Property(request => request.Address).HasMaxLength(ColumnLengths.Address);
        builder.Property(request => request.MaximumBudget).HasPrecision(ColumnLengths.MoneyPrecision, ColumnLengths.MoneyScale);

        builder.HasOne(request => request.Customer)
            .WithMany(user => user.ServiceRequests)
            .HasForeignKey(request => request.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.City)
            .WithMany(city => city.ServiceRequests)
            .HasForeignKey(request => request.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.ServiceCategory)
            .WithMany(category => category.ServiceRequests)
            .HasForeignKey(request => request.ServiceCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.TargetTradespersonProfile)
            .WithMany(profile => profile.TargetedServiceRequests)
            .HasForeignKey(request => request.TargetTradespersonProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
