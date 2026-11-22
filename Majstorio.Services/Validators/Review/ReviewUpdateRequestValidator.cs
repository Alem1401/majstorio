using FluentValidation;
using Majstorio.Model.Requests.Review;
using Majstorio.Services.Database.Configurations;

namespace Majstorio.Services.Validators.Review;

public class ReviewUpdateRequestValidator : AbstractValidator<ReviewUpdateRequest>
{
    public ReviewUpdateRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
        RuleFor(request => request.AppointmentId).GreaterThan(0);
        RuleFor(request => request.Rating).InclusiveBetween(1, 5);
        RuleFor(request => request.Comment).MaximumLength(ColumnLengths.LongText)
            .WithMessage($"Comment can have at most {ColumnLengths.LongText} characters.");
    }
}
