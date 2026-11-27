using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.PayPal;
using Majstorio.Services.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

public class PaymentServiceTests : IDisposable
{
    private readonly MajstorioDbContext _ctx = TestSupport.NewInMemoryContext();
    private readonly Scenario _s;
    private readonly FakePayPalClient _payPal = new();
    private readonly FakeMessagePublisher _publisher = new();
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _s = new Scenario(_ctx);
        _service = new PaymentService(_ctx, _payPal, _publisher);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task Start_CompletedJob_UsesTheOfferPriceAndReturnsTheApprovalPage()
    {
        var appointment = await AddAppointmentAsync();

        var result = await _service.StartPaymentAsync(TestSupport.As(_s.Customer), new() { AppointmentId = appointment.Id });

        Assert.True(result.IsSuccess, result.Message);
        Assert.Equal("https://paypal.test/approve", result.Value!.ApprovalUrl);
        var payment = await _ctx.Payments.SingleAsync();
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(100m, payment.Amount);
    }

    [Fact]
    public async Task Start_Again_KeepsOnePaymentPerJob()
    {
        var appointment = await AddAppointmentAsync();
        var customer = TestSupport.As(_s.Customer);
        var first = await _service.StartPaymentAsync(customer, new() { AppointmentId = appointment.Id });

        var second = await _service.StartPaymentAsync(customer, new() { AppointmentId = appointment.Id });

        Assert.Equal(first.Value!.PaymentId, second.Value!.PaymentId);
        Assert.Single(await _ctx.Payments.ToListAsync());
    }

    [Fact]
    public async Task Start_JobOfAnotherCustomer_IsNotFound()
    {
        var appointment = await AddAppointmentAsync();

        var result = await _service.StartPaymentAsync(TestSupport.As(TestSupport.NewUser()),
            new() { AppointmentId = appointment.Id });

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Start_JobNotCompleted_ReturnsConflict()
    {
        var appointment = await AddAppointmentAsync(AppointmentStatus.Confirmed);

        var result = await _service.StartPaymentAsync(TestSupport.As(_s.Customer), new() { AppointmentId = appointment.Id });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Capture_MarksPaidWithTheCapturedAmount_AndSecondCallChangesNothing()
    {
        var payment = await StartAsync();
        var customer = TestSupport.As(_s.Customer);

        var first = await _service.CapturePaymentAsync(customer, payment.Id);
        var second = await _service.CapturePaymentAsync(customer, payment.Id);

        Assert.True(first.IsSuccess, first.Message);
        Assert.True(second.IsSuccess);
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(100m, payment.CapturedAmount);
        Assert.Equal(1, _payPal.CaptureCalls);
        Assert.Equal(_s.Profile.UserId, Assert.Single(_publisher.Sent).UserId);
    }

    [Fact]
    public async Task Capture_NotApprovedInPayPal_StaysPending()
    {
        var payment = await StartAsync();
        _payPal.Fail = true;

        var result = await _service.CapturePaymentAsync(TestSupport.As(_s.Customer), payment.Id);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public async Task Start_AfterPaid_ReturnsConflict()
    {
        var payment = await PaidAsync();

        var result = await _service.StartPaymentAsync(TestSupport.As(_s.Customer),
            new() { AppointmentId = payment.AppointmentId });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Refund_RefundsTheCapturedAmountOnlyOnce()
    {
        var payment = await PaidAsync();
        var admin = TestSupport.As(_s.Administrator);

        var first = await _service.RefundPaymentAsync(admin, new() { Id = payment.Id, Reason = "Job was not done." });
        var second = await _service.RefundPaymentAsync(admin, new() { Id = payment.Id, Reason = "Again." });

        Assert.True(first.IsSuccess, first.Message);
        Assert.True(first.Value!.IsRefunded);
        Assert.Equal(ErrorType.Conflict, second.ErrorType);
        var refund = await _ctx.Refunds.SingleAsync();
        Assert.Equal(100m, refund.Amount);
        Assert.Equal(_s.Administrator.Id, refund.RequestedByUserId);
    }

    [Fact]
    public async Task Refund_UnpaidPayment_ReturnsConflict()
    {
        var payment = await StartAsync();

        var result = await _service.RefundPaymentAsync(TestSupport.As(_s.Administrator),
            new() { Id = payment.Id, Reason = "Reason" });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task GetPayments_FiltersByStatus()
    {
        await PaidAsync();

        var paid = (await _service.GetPaymentsAsync(new() { Status = (int)PaymentStatus.Paid })).Value!;
        var pending = (await _service.GetPaymentsAsync(new() { Status = (int)PaymentStatus.Pending })).Value!;

        Assert.Single(paid.Items);
        Assert.Empty(pending.Items);
    }

    private async Task<Payment> StartAsync()
    {
        var appointment = await AddAppointmentAsync();
        Assert.True((await _service.StartPaymentAsync(TestSupport.As(_s.Customer),
            new() { AppointmentId = appointment.Id })).IsSuccess);
        return await _ctx.Payments.SingleAsync();
    }

    private async Task<Payment> PaidAsync()
    {
        var payment = await StartAsync();
        Assert.True((await _service.CapturePaymentAsync(TestSupport.As(_s.Customer), payment.Id)).IsSuccess);
        return payment;
    }

    private async Task<Appointment> AddAppointmentAsync(AppointmentStatus status = AppointmentStatus.Completed)
    {
        var appointment = new Appointment
        {
            Offer = _s.AddOffer(status: OfferStatus.Accepted),
            StartUtc = DateTime.UtcNow.AddHours(-2), EndUtc = DateTime.UtcNow.AddHours(-1), Status = status
        };
        _ctx.Appointments.Add(appointment);
        await _ctx.SaveChangesAsync();
        return appointment;
    }
}

// Stands in for the PayPal sandbox in tests: it answers like PayPal would, without calling it.
// There is no real network call, so each method just awaits a finished task.
internal sealed class FakePayPalClient : IPayPalClient
{
    private decimal _orderAmount;

    public string ReturnUrl => "https://majstorio.test/paypal/return";
    public string CancelUrl => "https://majstorio.test/paypal/cancel";

    public int CaptureCalls { get; private set; }

    // Set to true to act as if the customer did not approve the payment.
    public bool Fail { get; set; }

    public async Task<PayPalOrder> CreateOrderAsync(decimal amount, Guid requestId)
    {
        await Task.CompletedTask;
        _orderAmount = amount;

        return new PayPalOrder
        {
            OrderId = "ORDER-" + requestId,
            ApprovalUrl = "https://paypal.test/approve"
        };
    }

    public async Task<PayPalCapture> CaptureOrderAsync(string orderId, Guid requestId)
    {
        await Task.CompletedTask;
        CaptureCalls++;

        if (Fail)
            throw new PayPalException("The payment was not approved.");

        return new PayPalCapture
        {
            CaptureId = "CAPTURE-" + requestId,
            Amount = _orderAmount
        };
    }

    public async Task<string> RefundCaptureAsync(string captureId, decimal amount, Guid requestId)
    {
        await Task.CompletedTask;
        return "REFUND-" + requestId;
    }
}
