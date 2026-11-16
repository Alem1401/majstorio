using FluentValidation;
using Majstorio.Model.Requests.Auth;

namespace Majstorio.Services.Validators.Auth;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(request => request.OldPassword).NotEmpty().WithMessage("Old password is required.");
        RuleFor(request => request.NewPassword).NotEmpty().WithMessage("New password is required.");
    }
}
