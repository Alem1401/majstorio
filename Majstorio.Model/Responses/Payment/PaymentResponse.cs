namespace Majstorio.Model.Responses.Payment;

public class PaymentResponse
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string TradespersonName { get; set; } = string.Empty;
    // The price of the accepted offer, in EUR.
    public decimal Amount { get; set; }
    // What PayPal confirmed it captured; null until the payment is paid.
    public decimal? CapturedAmount { get; set; }
    // 0 = Pending, 1 = Paid, 2 = Failed, 3 = Cancelled.
    public int Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public bool IsRefunded { get; set; }
    public string? RefundReason { get; set; }
}
