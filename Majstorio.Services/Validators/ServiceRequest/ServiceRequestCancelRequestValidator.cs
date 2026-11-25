using FluentValidation;
using Majstorio.Model.Requests.ServiceRequest;
using Majstorio.Services.Database.Configurations;

namespace Majstorio.Services.Validators.ServiceRequest;

public class ServiceRequestCancelRequestValidator : AbstractValidator<ServiceRequestCancelRequest>
{
    public ServiceRequestCancelRequestValidator()
    {
        RuleFor(request => request.Reason).MaximumLength(ColumnLengths.LongText);
    }
}
