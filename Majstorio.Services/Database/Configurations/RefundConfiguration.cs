using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majstorio.Services.Database.Configurations;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.Property(refund => refund.Amount).HasPrecision(ColumnLengths.MoneyPrecision, ColumnLengths.MoneyScale);
        builder.Property(refund => refund.Reason).HasMaxLength(ColumnLengths.LongText).IsRequired();
        builder.Property(refund => refund.PayPalRefundId).HasMaxLength(ColumnLengths.ExternalId);

        // One-to-one: a payment can be refunded in full only once.
        builder.HasOne(refund => refund.Payment)
            .WithOne(payment => payment.Refund)
            .HasForeignKey<Refund>(refund => refund.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(refund => refund.RequestedByUser)
            .WithMany(user => user.RequestedRefunds)
            .HasForeignKey(refund => refund.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(refund => refund.PayPalRefundId)
            .IsUnique()
            .HasFilter("[PayPalRefundId] IS NOT NULL");
        builder.HasIndex(refund => refund.IdempotencyKey).IsUnique();
    }
}
