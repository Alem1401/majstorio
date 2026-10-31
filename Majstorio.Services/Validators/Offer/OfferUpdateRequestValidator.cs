using FluentValidation;
using Majstorio.Model.Requests.Offer;
using Majstorio.Services.Enums;

namespace Majstorio.Services.Validators.Offer;

public class OfferUpdateRequestValidator : AbstractValidator<OfferUpdateRequest>
{
    public OfferUpdateRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
        RuleFor(request => request.ServiceRequestId).GreaterThan(0);
        RuleFor(request => request.TradespersonProfileId).GreaterThan(0);
        RuleFor(request => request.Price).GreaterThan(0).PrecisionScale(18, 2, true);
        RuleFor(request => request.ProposedStartUtc)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(value => value.Kind == DateTimeKind.Utc)
            .WithMessage("Proposed start must be in UTC.");
        RuleFor(request => request.ProposedStartUtc)
            .GreaterThan(_ => DateTime.UtcNow)
            .WithMessage("Proposed start must be in the future.")
            .When(request => request.Status is (int)OfferStatus.Pending or (int)OfferStatus.Accepted);
        RuleFor(request => request.DurationMinutes)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0)
            .Must((request, duration) => duration <= (DateTime.MaxValue - request.ProposedStartUtc).TotalMinutes)
            .WithMessage("Offer duration exceeds the supported date range.");
        RuleFor(request => request.Status)
            .Must(value => Enum.IsDefined(typeof(OfferStatus), value))
            .WithMessage("Offer status is invalid.");
        RuleFor(request => request.DecidedByUserId)
            .NotNull().GreaterThan(0)
            .When(request => request.Status != (int)OfferStatus.Pending);
        RuleFor(request => request.DecidedByUserId)
            .Null().When(request => request.Status == (int)OfferStatus.Pending);
        RuleFor(request => request.DecisionReason)
            .Null().When(request => request.Status == (int)OfferStatus.Pending);
    }
}
