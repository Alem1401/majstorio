using Majstorio.Model.Requests.Payment;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Payment;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;

public interface IPaymentService
{
    // Administrator: all payments, newest first.
    Task<Result<PagedResponse<PaymentResponse>>> GetPaymentsAsync(PaymentSearchRequest search);

    // Customer: creates the PayPal order for a completed job and returns the approval page.
    Task<Result<PaymentCheckoutResponse>> StartPaymentAsync(CurrentUser user, PaymentCreateRequest request);

    // Customer: after approving in PayPal, the API captures the order; only then is the payment Paid.
    Task<Result<PaymentResponse>> CapturePaymentAsync(CurrentUser user, int id);

    // Administrator: full refund of the captured amount, once per payment.
    Task<Result<PaymentResponse>> RefundPaymentAsync(CurrentUser user, PaymentRefundRequest request);
}
