using FluentValidation;
using Majstorio.Model.Requests.ServiceCategory;

namespace Majstorio.Services.Validators.ServiceCategory;

public class ServiceCategoryCreateRequestValidator : AbstractValidator<ServiceCategoryCreateRequest>
{
    public ServiceCategoryCreateRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Service category name is required.");
    }
}
