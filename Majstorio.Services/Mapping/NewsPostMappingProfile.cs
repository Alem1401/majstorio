using Majstorio.Model.Requests.NewsPost;
using Majstorio.Model.Responses.NewsPost;
using Majstorio.Services.Database;
using Mapster;

namespace Majstorio.Services.Mapping;

public class NewsPostMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<NewsPost, NewsPostResponse>();

        config.NewConfig<NewsPostCreateRequest, NewsPost>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.CreatedAtUtc)
            .Ignore(destination => destination.PublishedAtUtc)
            .Ignore(destination => destination.Author);

        config.NewConfig<NewsPostUpdateRequest, NewsPost>()
            .Ignore(destination => destination.CreatedAtUtc)
            .Ignore(destination => destination.PublishedAtUtc)
            .Ignore(destination => destination.Author);

        config.NewConfig<NewsPostDeleteRequest, NewsPost>()
            .Ignore(destination => destination.AuthorId)
            .Ignore(destination => destination.Title)
            .Ignore(destination => destination.Content)
            .Ignore(destination => destination.ImageUrl)
            .Ignore(destination => destination.CreatedAtUtc)
            .Ignore(destination => destination.PublishedAtUtc)
            .Ignore(destination => destination.Author);
    }
}
