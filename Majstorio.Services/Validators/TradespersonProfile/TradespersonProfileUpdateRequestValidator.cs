using FluentValidation;
using Majstorio.Model.Requests.TradespersonProfile;
using Majstorio.Services.Database.Configurations;

namespace Majstorio.Services.Validators.TradespersonProfile;

public class TradespersonProfileUpdateRequestValidator : AbstractValidator<TradespersonProfileUpdateRequest>
{
    public TradespersonProfileUpdateRequestValidator()
    {
        RuleFor(request => request.CityId).GreaterThan(0).WithMessage("City is required.");

        RuleFor(request => request.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(ColumnLengths.LongText)
            .WithMessage($"Description must be {ColumnLengths.LongText} characters or fewer.");
    }
}
