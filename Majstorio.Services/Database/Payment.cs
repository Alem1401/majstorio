using Majstorio.Services.Enums;

namespace Majstorio.Services.Database;

// One payment attempt per external order; failed attempts retain their provider references.
public class Payment : BaseEntity
{
    // Links to Appointment.Id; a job can have many attempts, but only one Pending or Paid.
    public int AppointmentId { get; set; }
    // Expected amount comes from the accepted offer, not from the mobile client.
    public decimal Amount { get; set; }
    // Actual provider-verified capture amount; full refunds use this value.
    public decimal? CapturedAmount { get; set; }
    // The proposed application charges and refunds in EUR.
    public string Currency { get; set; } = "EUR";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? PayPalOrderId { get; set; }
    public string? PayPalCaptureId { get; set; }
    // Keep this key unchanged when retrying creation of the same external order.
    public Guid CreateOrderRequestId { get; set; } = Guid.NewGuid();
    // A separate stable key for retrying capture of that order.
    public Guid CaptureRequestId { get; set; } = Guid.NewGuid();
    public DateTime? PaidAtUtc { get; set; }

    public Appointment Appointment { get; set; } = null!;
}
