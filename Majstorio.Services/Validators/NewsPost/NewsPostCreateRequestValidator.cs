using FluentValidation;
using Majstorio.Model.Requests.NewsPost;

namespace Majstorio.Services.Validators.NewsPost;

public class NewsPostCreateRequestValidator : AbstractValidator<NewsPostCreateRequest>
{
    public NewsPostCreateRequestValidator()
    {
        RuleFor(request => request.AuthorId).GreaterThan(0);
        RuleFor(request => request.Title)
            .NotEmpty().WithMessage("News post title is required.");
        RuleFor(request => request.Content)
            .NotEmpty().WithMessage("News post content is required.");
    }
}
