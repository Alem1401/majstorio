using FluentValidation;
using Majstorio.Model.Requests.Review;

namespace Majstorio.Services.Validators.Review;

public class ReviewSearchRequestValidator : SearchRequestValidator<ReviewSearchRequest>
{
    public ReviewSearchRequestValidator()
    {
        RuleFor(request => request.Rating)
            .InclusiveBetween(1, 5)
            .When(request => request.Rating.HasValue)
            .WithMessage("Rating must be between 1 and 5.");
    }
}
