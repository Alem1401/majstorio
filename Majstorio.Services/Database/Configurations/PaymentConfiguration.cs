using Majstorio.Services.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majstorio.Services.Database.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(payment => payment.Amount).HasPrecision(ColumnLengths.MoneyPrecision, ColumnLengths.MoneyScale);
        builder.Property(payment => payment.CapturedAmount).HasPrecision(ColumnLengths.MoneyPrecision, ColumnLengths.MoneyScale);
        builder.Property(payment => payment.Currency).HasMaxLength(ColumnLengths.Currency).IsRequired();
        builder.Property(payment => payment.PayPalOrderId).HasMaxLength(ColumnLengths.ExternalId);
        builder.Property(payment => payment.PayPalCaptureId).HasMaxLength(ColumnLengths.ExternalId);

        builder.HasOne(payment => payment.Appointment)
            .WithMany(appointment => appointment.Payments)
            .HasForeignKey(payment => payment.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Many attempts per job are allowed, but only one may be open or already paid.
        builder.HasIndex(payment => payment.AppointmentId)
            .IsUnique()
            .HasDatabaseName("IX_Payments_AppointmentId_Open")
            .HasFilter($"[Status] IN ({(int)PaymentStatus.Pending}, {(int)PaymentStatus.Paid})");

        // PayPal identifiers and our idempotency keys must never repeat.
        builder.HasIndex(payment => payment.PayPalOrderId)
            .IsUnique()
            .HasFilter("[PayPalOrderId] IS NOT NULL");
        builder.HasIndex(payment => payment.PayPalCaptureId)
            .IsUnique()
            .HasFilter("[PayPalCaptureId] IS NOT NULL");
        builder.HasIndex(payment => payment.CreateOrderRequestId).IsUnique();
        builder.HasIndex(payment => payment.CaptureRequestId).IsUnique();
    }
}
