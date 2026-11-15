using FluentValidation;
using Majstorio.Model.Requests.User;
using Majstorio.Services.Database.Configurations;
using Majstorio.Services.Enums;

namespace Majstorio.Services.Validators.User;

public class UserSearchRequestValidator : SearchRequestValidator<UserSearchRequest>
{
    public UserSearchRequestValidator()
    {
        RuleFor(request => request.Name)
            .MaximumLength(ColumnLengths.Name)
            .WithMessage($"The search text can have at most {ColumnLengths.Name} characters.");
        RuleFor(request => request.Role)
            .IsEnumName(typeof(UserRole), caseSensitive: false)
            .When(request => !string.IsNullOrWhiteSpace(request.Role))
            .WithMessage("Role must be Customer, Tradesperson or Administrator.");
    }
}
