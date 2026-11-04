using FluentValidation;
using Majstorio.Model.Requests;

namespace Majstorio.Services.Validators;

// Paging rules shared by every search request validator.
public abstract class SearchRequestValidator<T> : AbstractValidator<T> where T : BaseSearchRequest
{
    protected SearchRequestValidator()
    {
        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or greater.");
        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, BaseSearchRequest.MaxPageSize)
            .WithMessage($"Page size must be between 1 and {BaseSearchRequest.MaxPageSize}.");
    }
}
