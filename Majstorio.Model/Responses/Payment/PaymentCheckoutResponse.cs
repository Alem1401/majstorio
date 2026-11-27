namespace Majstorio.Model.Responses.Payment;

// What the mobile app needs to show the PayPal page inside the app and to see when the customer is done.
public class PaymentCheckoutResponse
{
    public int PaymentId { get; set; }
    // The PayPal page where the customer approves the payment.
    public string ApprovalUrl { get; set; } = string.Empty;
    // PayPal sends the customer to one of these when they approve or cancel; the app closes the page there.
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}
