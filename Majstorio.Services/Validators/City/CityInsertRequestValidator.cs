using FluentValidation;
using Majstorio.Model.Requests.City;

namespace Majstorio.Services.Validators.City;

public class CityInsertRequestValidator : AbstractValidator<CityInsertRequest>
{
    public CityInsertRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("City name is required.");
    }
}
