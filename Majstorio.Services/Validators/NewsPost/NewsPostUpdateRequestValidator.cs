using FluentValidation;
using Majstorio.Model.Requests.NewsPost;

namespace Majstorio.Services.Validators.NewsPost;

public class NewsPostUpdateRequestValidator : AbstractValidator<NewsPostUpdateRequest>
{
    public NewsPostUpdateRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
        RuleFor(request => request.AuthorId).GreaterThan(0);
        RuleFor(request => request.Title)
            .NotEmpty().WithMessage("News post title is required.");
        RuleFor(request => request.Content)
            .NotEmpty().WithMessage("News post content is required.");
        RuleFor(request => request.ImageUrl)
            .Must(value => string.IsNullOrWhiteSpace(value)
                || (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("News post image URL must be an absolute HTTP or HTTPS URL.");
    }
}
