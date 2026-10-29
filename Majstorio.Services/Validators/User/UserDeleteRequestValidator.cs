using FluentValidation;
using Majstorio.Model.Requests.User;

namespace Majstorio.Services.Validators.User;

public class UserDeleteRequestValidator : AbstractValidator<UserDeleteRequest>
{
    public UserDeleteRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
    }
}
