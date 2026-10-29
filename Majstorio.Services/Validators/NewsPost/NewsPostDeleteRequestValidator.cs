using FluentValidation;
using Majstorio.Model.Requests.NewsPost;

namespace Majstorio.Services.Validators.NewsPost;

public class NewsPostDeleteRequestValidator : AbstractValidator<NewsPostDeleteRequest>
{
    public NewsPostDeleteRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
    }
}
