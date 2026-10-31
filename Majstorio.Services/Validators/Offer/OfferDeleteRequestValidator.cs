using FluentValidation;
using Majstorio.Model.Requests.Offer;

namespace Majstorio.Services.Validators.Offer;

public class OfferDeleteRequestValidator : AbstractValidator<OfferDeleteRequest>
{
    public OfferDeleteRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
    }
}
