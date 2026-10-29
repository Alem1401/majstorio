using FluentValidation;
using Majstorio.Model.Requests.City;

namespace Majstorio.Services.Validators.City;

public class CityUpdateRequestValidator : AbstractValidator<CityUpdateRequest>
{
    public CityUpdateRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("City name is required.");
    }
}
