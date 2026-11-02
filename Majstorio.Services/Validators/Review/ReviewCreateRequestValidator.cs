using FluentValidation;
using Majstorio.Model.Requests.Review;

namespace Majstorio.Services.Validators.Review;

public class ReviewCreateRequestValidator : AbstractValidator<ReviewCreateRequest>
{
    public ReviewCreateRequestValidator()
    {
        RuleFor(request => request.AppointmentId).GreaterThan(0);
        RuleFor(request => request.Rating).InclusiveBetween(1, 5);
    }
}
