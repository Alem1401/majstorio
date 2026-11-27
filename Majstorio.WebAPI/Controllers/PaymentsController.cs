using Majstorio.Model.Requests.Payment;
using Majstorio.Services.Constants;
using Majstorio.WebAPI.Services;
using Majstorio.WebAPI.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public PaymentsController(IPaymentService paymentService, ResultToStatusCodeMapper resultMapper)
    {
        _paymentService = paymentService;
        _resultMapper = resultMapper;
    }

    // Administrative overview of payments and refunds for the desktop app.
    [Authorize(Roles = RoleNames.Administrator)]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PaymentSearchRequest search)
        => _resultMapper.Map(await _paymentService.GetPaymentsAsync(search));

    // Customer: opens the PayPal order for a completed job. Ownership is checked in the service.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PaymentCreateRequest request)
        => _resultMapper.Map(await _paymentService.StartPaymentAsync(User.ToCurrentUser(), request));

    // Customer: after approving on the PayPal page, the API captures the order and verifies it with PayPal.
    [HttpPost("{id:int}/capture")]
    public async Task<IActionResult> Capture(int id)
        => _resultMapper.Map(await _paymentService.CapturePaymentAsync(User.ToCurrentUser(), id));

    [Authorize(Roles = RoleNames.Administrator)]
    [HttpPost("{id:int}/refund")]
    public async Task<IActionResult> Refund(int id, [FromBody] PaymentRefundRequest request)
    {
        request.Id = id;
        return _resultMapper.Map(await _paymentService.RefundPaymentAsync(User.ToCurrentUser(), request));
    }
}
