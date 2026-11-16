using FluentValidation;
using Majstorio.Model.Requests.ServiceRequest;

namespace Majstorio.Services.Validators.ServiceRequest;

public class ServiceRequestDeleteRequestValidator : AbstractValidator<ServiceRequestDeleteRequest>
{
    public ServiceRequestDeleteRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
    }
}
