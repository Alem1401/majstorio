using Majstorio.Model.Messages;
using Majstorio.Model.Requests.Payment;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Payment;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Extensions;
using Majstorio.Services.PayPal;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

// PayPal payment of a completed job, and its full refund.
// The app never sends an amount and never marks anything paid: the amount is the accepted offer's price,
// and a payment is Paid only after the API itself captured it at PayPal.
public class PaymentService : IPaymentService
{
    private const string NotFoundMessage = "Payment not found.";
    private const string PayPalFailedMessage = "PayPal could not process the payment. Try again.";

    private readonly MajstorioDbContext _ctx;
    private readonly IPayPalClient _payPal;
    private readonly IMessagePublisher _publisher;

    public PaymentService(MajstorioDbContext ctx, IPayPalClient payPal, IMessagePublisher publisher)
    {
        _ctx = ctx;
        _payPal = payPal;
        _publisher = publisher;
    }

    public async Task<Result<PagedResponse<PaymentResponse>>> GetPaymentsAsync(PaymentSearchRequest search)
    {
        var query = PaymentsWithNames();

        if (search.Status.HasValue)
        {
            var status = (PaymentStatus)search.Status.Value;
            query = query.Where(payment => payment.Status == status);
        }

        var page = await query
            .OrderByDescending(payment => payment.CreatedAtUtc)
            .ThenByDescending(payment => payment.Id)
            .ToPagedAsync(search, payments => payments.Select(ToResponse).ToList());

        return Result<PagedResponse<PaymentResponse>>.Success(page);
    }

    public async Task<Result<PaymentCheckoutResponse>> StartPaymentAsync(CurrentUser user, PaymentCreateRequest request)
    {
        var appointment = await _ctx.Appointments
            .Include(appointment => appointment.Offer.ServiceRequest)
            .Include(appointment => appointment.Payments)
            .FirstOrDefaultAsync(appointment => appointment.Id == request.AppointmentId);

        // Another customer's job is reported as missing, so ids cannot be probed.
        if (appointment == null || appointment.Offer.ServiceRequest.CustomerId != user.Id)
            return Result<PaymentCheckoutResponse>.Failure(ErrorType.NotFound, "Appointment not found.");

        if (appointment.Status != AppointmentStatus.Completed)
            return Result<PaymentCheckoutResponse>.Failure(ErrorType.Conflict,
                "A job can be paid only after the tradesperson marks it as completed.");

        if (appointment.Payments.Any(payment => payment.Status == PaymentStatus.Paid))
            return Result<PaymentCheckoutResponse>.Failure(ErrorType.Conflict, "This job is already paid.");

        var amount = appointment.Offer.Price;
        var requestId = Guid.NewGuid();

        PayPalOrder order;
        try
        {
            order = await _payPal.CreateOrderAsync(amount, requestId);
        }
        catch (PayPalException)
        {
            return Result<PaymentCheckoutResponse>.Failure(ErrorType.Conflict, PayPalFailedMessage);
        }

        // A job has one open payment. If the customer starts again, the open payment gets the new order.
        var payment = appointment.Payments.FirstOrDefault(payment => payment.Status == PaymentStatus.Pending);
        if (payment == null)
        {
            payment = new Payment { AppointmentId = appointment.Id };
            _ctx.Payments.Add(payment);
        }

        payment.Amount = amount;
        payment.PayPalOrderId = order.OrderId;
        payment.CreateOrderRequestId = requestId;
        payment.CaptureRequestId = Guid.NewGuid();
        await _ctx.SaveChangesAsync();

        var response = new PaymentCheckoutResponse
        {
            PaymentId = payment.Id,
            ApprovalUrl = order.ApprovalUrl,
            ReturnUrl = _payPal.ReturnUrl,
            CancelUrl = _payPal.CancelUrl
        };
        return Result<PaymentCheckoutResponse>.Success(response);
    }

    public async Task<Result<PaymentResponse>> CapturePaymentAsync(CurrentUser user, int id)
    {
        var payment = await _ctx.Payments
            .Include(payment => payment.Appointment.Offer.ServiceRequest)
            .Include(payment => payment.Appointment.Offer.TradespersonProfile)
            .FirstOrDefaultAsync(payment => payment.Id == id);

        if (payment == null || payment.Appointment.Offer.ServiceRequest.CustomerId != user.Id)
            return Result<PaymentResponse>.Failure(ErrorType.NotFound, NotFoundMessage);

        // Confirming again changes nothing.
        if (payment.Status == PaymentStatus.Paid)
            return await GetPaymentAsync(payment.Id);

        if (payment.Status != PaymentStatus.Pending || payment.PayPalOrderId == null)
            return Result<PaymentResponse>.Failure(ErrorType.Conflict, "This payment is no longer open.");

        PayPalCapture capture;
        try
        {
            capture = await _payPal.CaptureOrderAsync(payment.PayPalOrderId, payment.CaptureRequestId);
        }
        catch (PayPalException)
        {
            return Result<PaymentResponse>.Failure(ErrorType.Conflict,
                "The payment was not approved in PayPal. Start the payment again.");
        }

        payment.Status = PaymentStatus.Paid;
        payment.PayPalCaptureId = capture.CaptureId;
        payment.CapturedAmount = capture.Amount;
        payment.PaidAtUtc = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();

        await _publisher.PublishAsync(new NotificationMessage
        {
            UserId = payment.Appointment.Offer.TradespersonProfile.UserId,
            ServiceRequestId = payment.Appointment.Offer.ServiceRequestId,
            Title = "Payment received",
            Message = $"The customer paid {capture.Amount:0.00} EUR for the job."
        });

        return await GetPaymentAsync(payment.Id);
    }

    public async Task<Result<PaymentResponse>> RefundPaymentAsync(CurrentUser user, PaymentRefundRequest request)
    {
        var payment = await _ctx.Payments
            .Include(payment => payment.Refund)
            .Include(payment => payment.Appointment.Offer.ServiceRequest)
            .FirstOrDefaultAsync(payment => payment.Id == request.Id);

        if (payment == null)
            return Result<PaymentResponse>.Failure(ErrorType.NotFound, NotFoundMessage);

        if (payment.Refund != null)
            return Result<PaymentResponse>.Failure(ErrorType.Conflict, "This payment was already refunded.");

        if (payment.Status != PaymentStatus.Paid || payment.PayPalCaptureId == null || payment.CapturedAmount == null)
            return Result<PaymentResponse>.Failure(ErrorType.Conflict, "Only a paid payment can be refunded.");

        // The refund is what PayPal actually charged, not a recalculated price.
        var amount = payment.CapturedAmount.Value;
        var requestId = Guid.NewGuid();

        string payPalRefundId;
        try
        {
            payPalRefundId = await _payPal.RefundCaptureAsync(payment.PayPalCaptureId, amount, requestId);
        }
        catch (PayPalException)
        {
            return Result<PaymentResponse>.Failure(ErrorType.Conflict, PayPalFailedMessage);
        }

        // Saved only after PayPal confirmed the refund.
        var refund = new Refund
        {
            PaymentId = payment.Id,
            RequestedByUserId = user.Id,
            Amount = amount,
            Reason = request.Reason.Trim(),
            Status = RefundStatus.Completed,
            PayPalRefundId = payPalRefundId,
            IdempotencyKey = requestId,
            ProcessedAtUtc = DateTime.UtcNow
        };
        _ctx.Refunds.Add(refund);
        await _ctx.SaveChangesAsync();

        await _publisher.PublishAsync(new NotificationMessage
        {
            UserId = payment.Appointment.Offer.ServiceRequest.CustomerId,
            ServiceRequestId = payment.Appointment.Offer.ServiceRequestId,
            Title = "Payment refunded",
            Message = $"{amount:0.00} EUR was refunded to your PayPal account."
        });

        return await GetPaymentAsync(payment.Id);
    }

    private async Task<Result<PaymentResponse>> GetPaymentAsync(int id)
    {
        var payment = await PaymentsWithNames().FirstAsync(payment => payment.Id == id);
        return Result<PaymentResponse>.Success(ToResponse(payment));
    }

    // Loads the customer, the tradesperson and the refund with the payments, so lists can show names.
    private IQueryable<Payment> PaymentsWithNames() => _ctx.Payments.AsNoTracking()
        .Include(payment => payment.Refund)
        .Include(payment => payment.Appointment.Offer.ServiceRequest.Customer)
        .Include(payment => payment.Appointment.Offer.TradespersonProfile.User);

    private static PaymentResponse ToResponse(Payment payment)
    {
        return new PaymentResponse
        {
            Id = payment.Id,
            AppointmentId = payment.AppointmentId,
            CustomerName = PersonNames.Full(payment.Appointment.Offer.ServiceRequest.Customer),
            TradespersonName = PersonNames.Full(payment.Appointment.Offer.TradespersonProfile.User),
            Amount = payment.Amount,
            CapturedAmount = payment.CapturedAmount,
            Status = (int)payment.Status,
            CreatedAtUtc = payment.CreatedAtUtc,
            PaidAtUtc = payment.PaidAtUtc,
            IsRefunded = payment.Refund != null,
            RefundReason = payment.Refund?.Reason
        };
    }
}
