using FluentValidation;
using Majstorio.Model.Requests.ServiceCategory;
using Majstorio.Services.Database.Configurations;

namespace Majstorio.Services.Validators.ServiceCategory;

public class ServiceCategorySearchRequestValidator : SearchRequestValidator<ServiceCategorySearchRequest>
{
    public ServiceCategorySearchRequestValidator()
    {
        RuleFor(request => request.Name)
            .MaximumLength(ColumnLengths.Name)
            .WithMessage($"The search text can have at most {ColumnLengths.Name} characters.");
    }
}
