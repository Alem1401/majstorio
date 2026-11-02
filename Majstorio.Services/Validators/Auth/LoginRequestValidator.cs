using FluentValidation;
using Majstorio.Model.Requests.Auth;

namespace Majstorio.Services.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.UserName)
            .NotEmpty().WithMessage("Username is required.");
        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
