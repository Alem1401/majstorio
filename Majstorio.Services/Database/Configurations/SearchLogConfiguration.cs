using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majstorio.Services.Database.Configurations;

public class SearchLogConfiguration : IEntityTypeConfiguration<SearchLog>
{
    public void Configure(EntityTypeBuilder<SearchLog> builder)
    {
        builder.Property(log => log.Query).HasMaxLength(ColumnLengths.ShortText);
        builder.Property(log => log.MinimumRating).HasPrecision(ColumnLengths.RatingPrecision, ColumnLengths.RatingScale);

        builder.HasOne(log => log.User)
            .WithMany(user => user.SearchLogs)
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(log => log.City)
            .WithMany(city => city.SearchLogs)
            .HasForeignKey(log => log.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(log => log.ServiceCategory)
            .WithMany(category => category.SearchLogs)
            .HasForeignKey(log => log.ServiceCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
