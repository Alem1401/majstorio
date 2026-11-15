using FluentValidation;
using Majstorio.Model.Requests.Offer;
using Majstorio.Services.Enums;

namespace Majstorio.Services.Validators.Offer;

public class OfferSearchRequestValidator : SearchRequestValidator<OfferSearchRequest>
{
    public OfferSearchRequestValidator()
    {
        RuleFor(request => request.Status)
            .Must(status => Enum.IsDefined(typeof(OfferStatus), status!.Value))
            .When(request => request.Status.HasValue)
            .WithMessage("Offer status is invalid. Use 0 (Pending), 1 (Accepted), 2 (Rejected), 3 (Withdrawn) or 4 (Closed).");
    }
}
