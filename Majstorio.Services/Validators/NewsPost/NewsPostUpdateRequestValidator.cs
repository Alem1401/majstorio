using FluentValidation;
using Majstorio.Model.Requests.NewsPost;
using Majstorio.Services.Database.Configurations;

namespace Majstorio.Services.Validators.NewsPost;

public class NewsPostUpdateRequestValidator : AbstractValidator<NewsPostUpdateRequest>
{
    public NewsPostUpdateRequestValidator()
    {
        RuleFor(request => request.AuthorId).GreaterThan(0);
        RuleFor(request => request.Title)
            .NotEmpty().WithMessage("News post title is required.")
            .MaximumLength(ColumnLengths.ShortText)
            .WithMessage($"News post title can have at most {ColumnLengths.ShortText} characters.");
        RuleFor(request => request.Content)
            .NotEmpty().WithMessage("News post content is required.");
        RuleFor(request => request.ImageUrl)
            .MaximumLength(ColumnLengths.Url)
            .WithMessage($"News post image URL can have at most {ColumnLengths.Url} characters.");
        RuleFor(request => request.ImageUrl)
            .Must(value => string.IsNullOrWhiteSpace(value)
                || (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("News post image URL must be an absolute HTTP or HTTPS URL.");
    }
}
