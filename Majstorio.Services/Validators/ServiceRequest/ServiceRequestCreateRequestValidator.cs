using FluentValidation;
using Majstorio.Model.Requests.ServiceRequest;
using Majstorio.Services.Database.Configurations;

namespace Majstorio.Services.Validators.ServiceRequest;

public class ServiceRequestCreateRequestValidator : AbstractValidator<ServiceRequestCreateRequest>
{
    public ServiceRequestCreateRequestValidator()
    {
        RuleFor(request => request.ServiceCategoryId).GreaterThan(0);
        RuleFor(request => request.CityId).GreaterThan(0);
        RuleFor(request => request.TargetTradespersonProfileId).GreaterThan(0)
            .When(request => request.TargetTradespersonProfileId.HasValue);
        RuleFor(request => request.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(ColumnLengths.LongText);
        RuleFor(request => request.Address).MaximumLength(ColumnLengths.Address);
        RuleFor(request => request.DesiredStartUtc)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(value => value.Kind == DateTimeKind.Utc).WithMessage("Desired start must be in UTC.")
            .GreaterThan(_ => DateTime.UtcNow).WithMessage("Desired start must be in the future.");
        RuleFor(request => request.MaximumBudget).GreaterThan(0).PrecisionScale(18, 2, true);
    }
}
