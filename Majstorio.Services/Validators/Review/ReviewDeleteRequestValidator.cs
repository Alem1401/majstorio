using FluentValidation;
using Majstorio.Model.Requests.Review;

namespace Majstorio.Services.Validators.Review;

public class ReviewDeleteRequestValidator : AbstractValidator<ReviewDeleteRequest>
{
    public ReviewDeleteRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
    }
}
