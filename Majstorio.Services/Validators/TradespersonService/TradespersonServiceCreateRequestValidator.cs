using FluentValidation;
using Majstorio.Model.Requests.TradespersonService;
using Majstorio.Services.Database.Configurations;

namespace Majstorio.Services.Validators.TradespersonService;

public class TradespersonServiceCreateRequestValidator : AbstractValidator<TradespersonServiceCreateRequest>
{
    public TradespersonServiceCreateRequestValidator()
    {
        RuleFor(request => request.TradespersonProfileId).GreaterThan(0).WithMessage("Tradesperson is required.");

        RuleFor(request => request.ServiceCategoryId).GreaterThan(0).WithMessage("Service category is required.");

        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Service name is required.")
            .MaximumLength(ColumnLengths.Name)
            .WithMessage($"Service name can have at most {ColumnLengths.Name} characters.");

        RuleFor(request => request.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(ColumnLengths.LongText)
            .WithMessage($"Description can have at most {ColumnLengths.LongText} characters.");

        RuleFor(request => request.MinimumPrice)
            .GreaterThan(0).WithMessage("Minimum price must be greater than 0.")
            .PrecisionScale(ColumnLengths.MoneyPrecision, ColumnLengths.MoneyScale, true)
            .WithMessage("Minimum price can have at most 2 decimals.");

        RuleFor(request => request.MaximumPrice)
            .GreaterThanOrEqualTo(request => request.MinimumPrice)
            .WithMessage("Maximum price cannot be lower than the minimum price.")
            .PrecisionScale(ColumnLengths.MoneyPrecision, ColumnLengths.MoneyScale, true)
            .WithMessage("Maximum price can have at most 2 decimals.");

        RuleFor(request => request.EstimatedDurationMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than 0 minutes.");
    }
}
