namespace Majstorio.Services.PayPal;

// A PayPal order that was just created.
public class PayPalOrder
{
    public string OrderId { get; set; } = string.Empty;
    // The PayPal page where the customer approves the payment.
    public string ApprovalUrl { get; set; } = string.Empty;
}
