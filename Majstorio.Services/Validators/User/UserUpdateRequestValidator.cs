using FluentValidation;
using Majstorio.Model.Requests.User;
using Majstorio.Services.Database.Configurations;
using Majstorio.Services.Enums;

namespace Majstorio.Services.Validators.User;

public class UserUpdateRequestValidator : AbstractValidator<UserUpdateRequest>
{
    public UserUpdateRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
        RuleFor(request => request.UserName)
            .NotEmpty().WithMessage("User name is required.")
            .MaximumLength(ColumnLengths.Name)
            .WithMessage($"User name can have at most {ColumnLengths.Name} characters.");
        RuleFor(request => request.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address, for example name@example.com.");
        RuleFor(request => request.PhoneNumber)
            .Matches(@"^\+?[0-9 /-]{6,20}$")
            .When(request => !string.IsNullOrWhiteSpace(request.PhoneNumber))
            .WithMessage("Phone number can contain 6 to 20 digits, spaces, / or -, with an optional leading +, for example +387 61 123 456.");
        RuleFor(request => request.FirstName).NotEmpty().WithMessage("First name is required.");
        RuleFor(request => request.LastName).NotEmpty().WithMessage("Last name is required.");
        RuleFor(request => request.Role)
            .IsEnumName(typeof(UserRole), caseSensitive: false)
            .When(request => request.Role != null)
            .WithMessage("Role must be Customer, Tradesperson or Administrator.");
    }
}
