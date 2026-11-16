using FluentValidation;
using Majstorio.Model.Requests.ServiceRequest;
using Majstorio.Services.Enums;

namespace Majstorio.Services.Validators.ServiceRequest;

public class ServiceRequestSearchRequestValidator : SearchRequestValidator<ServiceRequestSearchRequest>
{
    public ServiceRequestSearchRequestValidator()
    {
        RuleFor(request => request.Status)
            .Must(status => Enum.IsDefined(typeof(RequestStatus), status!.Value))
            .When(request => request.Status.HasValue)
            .WithMessage("Request status is invalid. Use 0 (Created), 1 (OfferAccepted), 2 (AppointmentConfirmed), 3 (Completed) or 4 (Cancelled).");
        RuleFor(request => request.ToUtc)
            .GreaterThanOrEqualTo(request => request.FromUtc)
            .When(request => request.FromUtc.HasValue && request.ToUtc.HasValue)
            .WithMessage("The end of the period cannot be before its start.");
    }
}
