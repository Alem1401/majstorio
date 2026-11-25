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

        RuleFor(request => request)
            .Must(request => request.AvailableFromUtc.HasValue == request.AvailableToUtc.HasValue)
            .WithMessage("Available from and available to must be set together.");

        RuleFor(request => request.AvailableToUtc)
            .GreaterThan(request => request.AvailableFromUtc)
            .When(request => request.AvailableFromUtc.HasValue && request.AvailableToUtc.HasValue)
            .WithMessage("Available to must be after available from.");
    }
}
