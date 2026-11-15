using FluentValidation;
using Majstorio.Model.Requests.Appointment;
using Majstorio.Services.Enums;

namespace Majstorio.Services.Validators.Appointment;

public class AppointmentSearchRequestValidator : SearchRequestValidator<AppointmentSearchRequest>
{
    public AppointmentSearchRequestValidator()
    {
        RuleFor(request => request.Status)
            .Must(status => Enum.IsDefined(typeof(AppointmentStatus), status!.Value))
            .When(request => request.Status.HasValue)
            .WithMessage("Appointment status is invalid. Use 0 (Pending), 1 (Confirmed), 2 (Cancelled) or 3 (Completed).");
    }
}
