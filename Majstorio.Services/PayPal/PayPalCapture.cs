namespace Majstorio.Services.PayPal;

// The money PayPal actually took from the customer.
public class PayPalCapture
{
    public string CaptureId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
