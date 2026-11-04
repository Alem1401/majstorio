using FluentValidation;
using Majstorio.Model.Requests.City;
using Majstorio.Services.Database.Configurations;

namespace Majstorio.Services.Validators.City;

public class CityUpdateRequestValidator : AbstractValidator<CityUpdateRequest>
{
    public CityUpdateRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("City name is required.")
            .MaximumLength(ColumnLengths.Name)
            .WithMessage($"City name can have at most {ColumnLengths.Name} characters.");
    }
}
