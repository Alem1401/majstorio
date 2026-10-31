using FluentValidation;
using Majstorio.Model.Requests.Appointment;
using Majstorio.Services.Enums;

namespace Majstorio.Services.Validators.Appointment;

public class AppointmentUpdateRequestValidator : AbstractValidator<AppointmentUpdateRequest>
{
    public AppointmentUpdateRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
        RuleFor(request => request.OfferId).GreaterThan(0);
        RuleFor(request => request.StartUtc)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(value => value.Kind == DateTimeKind.Utc)
            .WithMessage("Appointment start must be in UTC.");
        RuleFor(request => request.EndUtc)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(value => value.Kind == DateTimeKind.Utc)
            .WithMessage("Appointment end must be in UTC.")
            .GreaterThan(request => request.StartUtc)
            .WithMessage("Appointment end must be after its start.");
        RuleFor(request => request.Status)
            .Must(value => Enum.IsDefined(typeof(AppointmentStatus), value))
            .WithMessage("Appointment status is invalid.");
    }
}
