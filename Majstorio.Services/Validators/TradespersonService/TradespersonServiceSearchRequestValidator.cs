using FluentValidation;
using Majstorio.Model.Requests.TradespersonService;
using Majstorio.Services.Database.Configurations;

namespace Majstorio.Services.Validators.TradespersonService;

public class TradespersonServiceSearchRequestValidator : SearchRequestValidator<TradespersonServiceSearchRequest>
{
    public TradespersonServiceSearchRequestValidator()
    {
        RuleFor(request => request.Name)
            .MaximumLength(ColumnLengths.Name)
            .WithMessage($"The search text can have at most {ColumnLengths.Name} characters.");
    }
}
