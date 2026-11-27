using FluentValidation;
using Majstorio.Model.Requests.Payment;

namespace Majstorio.Services.Validators.Payment;

public class PaymentCreateRequestValidator : AbstractValidator<PaymentCreateRequest>
{
    public PaymentCreateRequestValidator()
    {
        RuleFor(request => request.AppointmentId)
            .GreaterThan(0).WithMessage("Appointment is required.");
    }
}
