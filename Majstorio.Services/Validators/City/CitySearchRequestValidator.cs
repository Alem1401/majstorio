using FluentValidation;
using Majstorio.Model.Requests.City;
using Majstorio.Services.Database.Configurations;

namespace Majstorio.Services.Validators.City;

public class CitySearchRequestValidator : SearchRequestValidator<CitySearchRequest>
{
    public CitySearchRequestValidator()
    {
        RuleFor(request => request.Name)
            .MaximumLength(ColumnLengths.Name)
            .WithMessage($"The search text can have at most {ColumnLengths.Name} characters.");
    }
}
