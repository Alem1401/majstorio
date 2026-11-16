using FluentValidation;
using Majstorio.Model.Requests.TradespersonProfile;

namespace Majstorio.Services.Validators.TradespersonProfile;

public class TradespersonProfileSearchRequestValidator : SearchRequestValidator<TradespersonProfileSearchRequest>
{
    public TradespersonProfileSearchRequestValidator()
    {
        RuleFor(request => request.MinimumRating)
            .InclusiveBetween(1, 5)
            .When(request => request.MinimumRating.HasValue)
            .WithMessage("Minimum rating must be between 1 and 5.");
    }
}
