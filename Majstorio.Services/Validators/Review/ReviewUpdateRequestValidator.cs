using FluentValidation;
using Majstorio.Model.Requests.Review;

namespace Majstorio.Services.Validators.Review;

public class ReviewUpdateRequestValidator : AbstractValidator<ReviewUpdateRequest>
{
    public ReviewUpdateRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
        RuleFor(request => request.AppointmentId).GreaterThan(0);
        RuleFor(request => request.Rating).InclusiveBetween(1, 5);
    }
}
