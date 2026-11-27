using FluentValidation;
using Majstorio.Model.Requests.Payment;
using Majstorio.Services.Enums;

namespace Majstorio.Services.Validators.Payment;

public class PaymentSearchRequestValidator : SearchRequestValidator<PaymentSearchRequest>
{
    public PaymentSearchRequestValidator()
    {
        RuleFor(request => request.Status)
            .Must(status => Enum.IsDefined(typeof(PaymentStatus), status!.Value))
            .When(request => request.Status.HasValue)
            .WithMessage("Payment status is invalid. Use 0 (Pending), 1 (Paid), 2 (Failed) or 3 (Cancelled).");
    }
}
