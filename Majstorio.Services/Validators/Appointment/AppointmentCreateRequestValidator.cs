using FluentValidation;
using Majstorio.Model.Requests.Appointment;

namespace Majstorio.Services.Validators.Appointment;

public class AppointmentCreateRequestValidator : AbstractValidator<AppointmentCreateRequest>
{
    public AppointmentCreateRequestValidator()
    {
        RuleFor(request => request.OfferId).GreaterThan(0);
        RuleFor(request => request.StartUtc)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(value => value.Kind == DateTimeKind.Utc)
            .WithMessage("Appointment start must be in UTC.")
            .GreaterThan(_ => DateTime.UtcNow)
            .WithMessage("Appointment start must be in the future.");
        RuleFor(request => request.EndUtc)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(value => value.Kind == DateTimeKind.Utc)
            .WithMessage("Appointment end must be in UTC.")
            .GreaterThan(request => request.StartUtc)
            .WithMessage("Appointment end must be after its start.");
    }
}
