using FluentValidation;
using Majstorio.Model.Requests.Report;

namespace Majstorio.Services.Validators.Report;

public class TradespersonReportRequestValidator : AbstractValidator<TradespersonReportRequest>
{
    public TradespersonReportRequestValidator()
    {
        RuleFor(request => request.ToUtc)
            .GreaterThan(request => request.FromUtc)
            .When(request => request.FromUtc.HasValue && request.ToUtc.HasValue)
            .WithMessage("The end of the period must be after its start.");
        RuleFor(request => request.CityId)
            .GreaterThan(0).When(request => request.CityId.HasValue)
            .WithMessage("City id must be greater than 0.");
    }
}
