using FluentValidation;
using Majstorio.Model.Requests.Auth;

namespace Majstorio.Services.Validators.Auth;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(request => request.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");
        RuleFor(request => request.Token).NotEmpty().WithMessage("Reset token is required.");
        RuleFor(request => request.NewPassword).NotEmpty().WithMessage("New password is required.");
    }
}
