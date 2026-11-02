using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majstorio.Services.Database.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.UserName).HasMaxLength(ColumnLengths.Email).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(ColumnLengths.Email).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(ColumnLengths.Hash).IsRequired();
        builder.Property(user => user.PasswordSalt).HasMaxLength(ColumnLengths.Hash).IsRequired();
        builder.Property(user => user.PhoneNumber).HasMaxLength(ColumnLengths.Phone);
        builder.Property(user => user.FirstName).HasMaxLength(ColumnLengths.Name).IsRequired();
        builder.Property(user => user.LastName).HasMaxLength(ColumnLengths.Name).IsRequired();
        builder.Property(user => user.ProfileImageUrl).HasMaxLength(ColumnLengths.Url);
        builder.Property(user => user.PasswordResetTokenHash).HasMaxLength(ColumnLengths.Hash);

        builder.HasIndex(user => user.UserName).IsUnique();
        builder.HasIndex(user => user.Email).IsUnique();
    }
}
