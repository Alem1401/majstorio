using FluentValidation;
using Majstorio.Model.Requests.Offer;

namespace Majstorio.Services.Validators.Offer;

public class OfferCreateRequestValidator : AbstractValidator<OfferCreateRequest>
{
    public OfferCreateRequestValidator()
    {
        RuleFor(request => request.ServiceRequestId).GreaterThan(0);
        RuleFor(request => request.TradespersonProfileId).GreaterThan(0);
        RuleFor(request => request.Price).GreaterThan(0).PrecisionScale(18, 2, true);
        RuleFor(request => request.ProposedStartUtc)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(value => value.Kind == DateTimeKind.Utc)
            .WithMessage("Proposed start must be in UTC.")
            .GreaterThan(_ => DateTime.UtcNow)
            .WithMessage("Proposed start must be in the future.");
        RuleFor(request => request.DurationMinutes)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0)
            .Must((request, duration) => duration <= (DateTime.MaxValue - request.ProposedStartUtc).TotalMinutes)
            .WithMessage("Offer duration exceeds the supported date range.");
    }
}
