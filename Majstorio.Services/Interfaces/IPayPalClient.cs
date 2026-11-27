using Majstorio.Services.PayPal;

// The PayPal sandbox. Every method throws PayPalException when PayPal refuses the call.
public interface IPayPalClient
{
    // PayPal sends the customer to these addresses after approving or cancelling.
    string ReturnUrl { get; }
    string CancelUrl { get; }

    Task<PayPalOrder> CreateOrderAsync(decimal amount, Guid requestId);
    Task<PayPalCapture> CaptureOrderAsync(string orderId, Guid requestId);
    // Returns the PayPal refund id.
    Task<string> RefundCaptureAsync(string captureId, decimal amount, Guid requestId);
}
