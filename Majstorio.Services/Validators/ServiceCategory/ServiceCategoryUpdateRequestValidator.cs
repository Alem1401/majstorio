using FluentValidation;
using Majstorio.Model.Requests.ServiceCategory;

namespace Majstorio.Services.Validators.ServiceCategory;

public class ServiceCategoryUpdateRequestValidator : AbstractValidator<ServiceCategoryUpdateRequest>
{
    public ServiceCategoryUpdateRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Service category name is required.");
    }
}
