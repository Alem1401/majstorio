using FluentValidation;
using Majstorio.Model.Requests.Report;
using Majstorio.Services.Enums;

namespace Majstorio.Services.Validators.Report;

public class RequestReportRequestValidator : AbstractValidator<RequestReportRequest>
{
    public RequestReportRequestValidator()
    {
        RuleFor(request => request.ToUtc)
            .GreaterThan(request => request.FromUtc)
            .When(request => request.FromUtc.HasValue && request.ToUtc.HasValue)
            .WithMessage("The end of the period must be after its start.");
        RuleFor(request => request.CityId)
            .GreaterThan(0).When(request => request.CityId.HasValue)
            .WithMessage("City id must be greater than 0.");
        RuleFor(request => request.ServiceCategoryId)
            .GreaterThan(0).When(request => request.ServiceCategoryId.HasValue)
            .WithMessage("Service category id must be greater than 0.");
        RuleFor(request => request.Status)
            .Must(status => Enum.IsDefined(typeof(RequestStatus), status!.Value))
            .When(request => request.Status.HasValue)
            .WithMessage("Request status is invalid. Use 0 (Created), 1 (OfferAccepted), 2 (AppointmentConfirmed), 3 (Completed) or 4 (Cancelled).");
    }
}
