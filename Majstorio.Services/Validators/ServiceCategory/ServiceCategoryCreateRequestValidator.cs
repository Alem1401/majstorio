using FluentValidation;
using Majstorio.Model.Requests.ServiceCategory;
using Majstorio.Services.Database.Configurations;

namespace Majstorio.Services.Validators.ServiceCategory;

public class ServiceCategoryCreateRequestValidator : AbstractValidator<ServiceCategoryCreateRequest>
{
    public ServiceCategoryCreateRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Service category name is required.")
            .MaximumLength(ColumnLengths.Name)
            .WithMessage($"Service category name can have at most {ColumnLengths.Name} characters.");
    }
}
