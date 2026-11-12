using FluentValidation;
using Majstorio.Model.Requests.NewsPost;
using Majstorio.Services.Database.Configurations;

namespace Majstorio.Services.Validators.NewsPost;

public class NewsPostSearchRequestValidator : SearchRequestValidator<NewsPostSearchRequest>
{
    public NewsPostSearchRequestValidator()
    {
        RuleFor(request => request.Title)
            .MaximumLength(ColumnLengths.ShortText)
            .WithMessage($"The search text can have at most {ColumnLengths.ShortText} characters.");
        RuleFor(request => request.AuthorId)
            .GreaterThan(0).When(request => request.AuthorId.HasValue)
            .WithMessage("Author id must be greater than 0.");
    }
}
