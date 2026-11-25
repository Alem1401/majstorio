using FluentValidation;
using Majstorio.Model.Requests.TradespersonProfile;

namespace Majstorio.Services.Validators.TradespersonProfile;

public class TradespersonProfileActiveRequestValidator : AbstractValidator<TradespersonProfileActiveRequest>
{
    public TradespersonProfileActiveRequestValidator()
    {
    }
}
