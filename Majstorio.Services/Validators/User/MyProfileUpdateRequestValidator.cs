using System.Text.RegularExpressions;
using FluentValidation;
using Majstorio.Model.Requests.User;
using Majstorio.Services.Database.Configurations;

namespace Majstorio.Services.Validators.User;

public class MyProfileUpdateRequestValidator : AbstractValidator<MyProfileUpdateRequest>
{
    public MyProfileUpdateRequestValidator()
    {
        RuleFor(request => request.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(ColumnLengths.Email)
            .EmailAddress().WithMessage("Email must be a valid email address, for example name@example.com.");
        RuleFor(request => request.PhoneNumber)
            .Must(BeAPhoneNumber!)
            .When(request => !string.IsNullOrWhiteSpace(request.PhoneNumber))
            .WithMessage("Phone number can contain 6 to 20 digits, spaces, / or -, with an optional leading +, for example +387 61 123 456.");
        RuleFor(request => request.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(ColumnLengths.Name);
        RuleFor(request => request.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(ColumnLengths.Name);
    }

    // An optional leading "+", then only digits, spaces, "/" and "-". The 6 to 20 limit counts digits,
    // so separators neither make "------" valid nor push a real number with many separators over the limit.
    private static bool BeAPhoneNumber(string phone)
    {
        var text = phone.Trim();
        return text.Length <= ColumnLengths.Phone
            && Regex.IsMatch(text, @"^\+?[0-9 /-]+$")
            && text.Count(char.IsAsciiDigit) is >= 6 and <= 20;
    }
}
