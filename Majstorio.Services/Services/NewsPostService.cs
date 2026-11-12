using Majstorio.Model.Requests.NewsPost;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.NewsPost;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Extensions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

public class NewsPostService : INewsPostService
{
    private const string NotFoundMessage = "News post not found.";

    private readonly MajstorioDbContext _ctx;
    private readonly IMapper _mapper;

    public NewsPostService(MajstorioDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<Result<PagedResponse<NewsPostResponse>>> GetNewsPostsAsync(NewsPostSearchRequest search)
    {
        var query = _ctx.NewsPosts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search.Title))
        {
            var title = search.Title.Trim();
            query = query.Where(newsPost => newsPost.Title.Contains(title));
        }

        if (search.AuthorId.HasValue)
            query = query.Where(newsPost => newsPost.AuthorId == search.AuthorId.Value);

        var page = await query
            .OrderByDescending(newsPost => newsPost.PublishedAtUtc)
            .ThenByDescending(newsPost => newsPost.Id)
            .ToPagedAsync(search, newsPosts => _mapper.Map<List<NewsPostResponse>>(newsPosts));

        return Result<PagedResponse<NewsPostResponse>>.Success(page);
    }

    public async Task<Result<NewsPostResponse>> GetNewsPostByIdAsync(int id)
    {
        var newsPost = await _ctx.NewsPosts.AsNoTracking().FirstOrDefaultAsync(newsPost => newsPost.Id == id);
        if (newsPost == null)
            return Result<NewsPostResponse>.Failure(ErrorType.NotFound, NotFoundMessage);

        return Result<NewsPostResponse>.Success(_mapper.Map<NewsPostResponse>(newsPost));
    }

    public async Task<Result<NewsPostResponse>> CreateNewsPostAsync(NewsPostCreateRequest request)
    {
        var validation = await ValidateAuthorAsync(request.AuthorId);
        if (!validation.IsSuccess)
            return Result<NewsPostResponse>.Failure(validation.ErrorType!.Value, validation.Message!);

        var newsPost = _mapper.Map<NewsPost>(request);
        NormalizeNewsPost(newsPost);
        _ctx.NewsPosts.Add(newsPost);
        await _ctx.SaveChangesAsync();
        return Result<NewsPostResponse>.Success(_mapper.Map<NewsPostResponse>(newsPost));
    }

    public async Task<Result> UpdateNewsPostAsync(int id, NewsPostUpdateRequest request)
    {
        var newsPost = await _ctx.NewsPosts.FirstOrDefaultAsync(newsPost => newsPost.Id == id);
        if (newsPost == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        var validation = await ValidateAuthorAsync(request.AuthorId);
        if (!validation.IsSuccess)
            return validation;

        _mapper.Map(request, newsPost);
        NormalizeNewsPost(newsPost);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteNewsPostAsync(NewsPostDeleteRequest request)
    {
        var newsPost = await _ctx.NewsPosts.FirstOrDefaultAsync(newsPost => newsPost.Id == request.Id);
        if (newsPost == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        _ctx.NewsPosts.Remove(newsPost);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    private async Task<Result> ValidateAuthorAsync(int authorId)
    {
        var author = await _ctx.ApplicationUsers.FirstOrDefaultAsync(user => user.Id == authorId);
        if (author == null)
            return Result.Failure(ErrorType.NotFound, "Author not found.");

        if (!author.IsActive)
            return Result.Failure(ErrorType.Forbidden, "An inactive author cannot publish or update news posts.");

        return Result.Success();
    }

    private static void NormalizeNewsPost(NewsPost newsPost)
    {
        newsPost.Title = newsPost.Title.Trim();
        newsPost.Content = newsPost.Content.Trim();
        newsPost.ImageUrl = newsPost.ImageUrl?.Trim() ?? string.Empty;
    }
}
