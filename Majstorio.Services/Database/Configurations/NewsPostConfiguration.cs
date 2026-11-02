using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majstorio.Services.Database.Configurations;

public class NewsPostConfiguration : IEntityTypeConfiguration<NewsPost>
{
    public void Configure(EntityTypeBuilder<NewsPost> builder)
    {
        builder.Property(post => post.Title).HasMaxLength(ColumnLengths.ShortText).IsRequired();
        builder.Property(post => post.Content).IsRequired();
        builder.Property(post => post.ImageUrl).HasMaxLength(ColumnLengths.Url).IsRequired();

        builder.HasOne(post => post.Author)
            .WithMany(user => user.NewsPosts)
            .HasForeignKey(post => post.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
