using FluentValidation;
using Majstorio.Model.Requests.User;

namespace Majstorio.Services.Validators.User;

public class UserCreateRequestValidator : AbstractValidator<UserCreateRequest>
{
    public UserCreateRequestValidator()
    {
        RuleFor(request => request.UserName).NotEmpty().WithMessage("User name is required.");
        RuleFor(request => request.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");
        RuleFor(request => request.Password).NotEmpty().WithMessage("Password is required.");
        RuleFor(request => request.FirstName).NotEmpty().WithMessage("First name is required.");
        RuleFor(request => request.LastName).NotEmpty().WithMessage("Last name is required.");
    }
}
