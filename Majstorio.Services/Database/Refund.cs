using Majstorio.Services.Enums;

namespace Majstorio.Services.Database;

// An optional full return of a payment; the intended amount equals the original captured amount.
public class Refund : BaseEntity
{
    // Links to Payment.Id; a payment has zero or one full refund.
    public int PaymentId { get; set; }
    // Links to ApplicationUser.Id; records who initiated the authorized refund.
    public int RequestedByUserId { get; set; }
    // Must equal the verified Payment.CapturedAmount for this full-refund design.
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public string? PayPalRefundId { get; set; }
    // Retry the same refund row with the same key instead of creating another refund.
    public Guid IdempotencyKey { get; set; } = Guid.NewGuid();
    public DateTime? ProcessedAtUtc { get; set; }

    public Payment Payment { get; set; } = null!;
    public ApplicationUser RequestedByUser { get; set; } = null!;
}
