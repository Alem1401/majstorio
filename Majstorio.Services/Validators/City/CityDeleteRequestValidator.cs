using FluentValidation;
using Majstorio.Model.Requests.City;

namespace Majstorio.Services.Validators.City;

public class CityDeleteRequestValidator : AbstractValidator<CityDeleteRequest>
{
    public CityDeleteRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
    }
}
