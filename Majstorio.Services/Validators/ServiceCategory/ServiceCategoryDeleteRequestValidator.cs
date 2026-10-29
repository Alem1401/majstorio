using FluentValidation;
using Majstorio.Model.Requests.ServiceCategory;

namespace Majstorio.Services.Validators.ServiceCategory;

public class ServiceCategoryDeleteRequestValidator : AbstractValidator<ServiceCategoryDeleteRequest>
{
    public ServiceCategoryDeleteRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
    }
}
