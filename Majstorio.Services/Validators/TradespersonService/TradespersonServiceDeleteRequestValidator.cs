using FluentValidation;
using Majstorio.Model.Requests.TradespersonService;

namespace Majstorio.Services.Validators.TradespersonService;

public class TradespersonServiceDeleteRequestValidator : AbstractValidator<TradespersonServiceDeleteRequest>
{
    public TradespersonServiceDeleteRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
    }
}
