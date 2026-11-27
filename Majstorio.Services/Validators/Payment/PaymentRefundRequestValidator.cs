using FluentValidation;
using Majstorio.Model.Requests.Payment;
using Majstorio.Services.Database.Configurations;

namespace Majstorio.Services.Validators.Payment;

public class PaymentRefundRequestValidator : AbstractValidator<PaymentRefundRequest>
{
    public PaymentRefundRequestValidator()
    {
        RuleFor(request => request.Reason)
            .Cascade(CascadeMode.Stop)
            .Must(reason => !string.IsNullOrWhiteSpace(reason)).WithMessage("Refund reason is required.")
            .MaximumLength(ColumnLengths.LongText)
            .WithMessage($"Refund reason can have at most {ColumnLengths.LongText} characters.");
    }
}
