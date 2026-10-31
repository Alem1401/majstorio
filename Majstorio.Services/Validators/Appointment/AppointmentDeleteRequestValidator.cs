using FluentValidation;
using Majstorio.Model.Requests.Appointment;

namespace Majstorio.Services.Validators.Appointment;

public class AppointmentDeleteRequestValidator : AbstractValidator<AppointmentDeleteRequest>
{
    public AppointmentDeleteRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
    }
}
