using Majstorio.Model.Requests.NewsPost;
using Majstorio.Model.Responses.NewsPost;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

public class NewsPostService : INewsPostService
{
    private readonly MajstorioDbContext _ctx;
    private readonly IMapper _mapper;

    public NewsPostService(MajstorioDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<Result> CreateNewsPostAsync(NewsPostCreateRequest request)
    {
        var validation = await ValidateAuthorAsync(request.AuthorId);
        if (!validation.IsSuccess)
            return validation;

        var newsPost = _mapper.Map<NewsPost>(request);
        NormalizeNewsPost(newsPost);
        await _ctx.NewsPosts.AddAsync(newsPost);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteNewsPostAsync(NewsPostDeleteRequest request)
    {
        var newsPost = await _ctx.NewsPosts.FirstOrDefaultAsync(newsPost => newsPost.Id == request.Id);
        if (newsPost == null)
            return Result.Failure(ErrorType.NotFound, "News post not found.");

        _ctx.NewsPosts.Remove(newsPost);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<NewsPostResponse>>> GetAllNewsPostsAsync()
    {
        var newsPosts = await _ctx.NewsPosts
            .OrderByDescending(newsPost => newsPost.PublishedAtUtc)
            .ThenByDescending(newsPost => newsPost.Id)
            .ToListAsync();
        return Result<List<NewsPostResponse>>.Success(
            _mapper.Map<List<NewsPostResponse>>(newsPosts));
    }

    public async Task<Result<NewsPostResponse>> GetNewsPostByIdAsync(int id)
    {
        var newsPost = await _ctx.NewsPosts.FirstOrDefaultAsync(newsPost => newsPost.Id == id);
        if (newsPost == null)
            return Result<NewsPostResponse>.Failure(ErrorType.NotFound, "News post not found.");

        return Result<NewsPostResponse>.Success(_mapper.Map<NewsPostResponse>(newsPost));
    }

    public async Task<Result> UpdateNewsPostAsync(NewsPostUpdateRequest request)
    {
        var newsPost = await _ctx.NewsPosts.FirstOrDefaultAsync(newsPost => newsPost.Id == request.Id);
        if (newsPost == null)
            return Result.Failure(ErrorType.NotFound, "News post not found.");

        var validation = await ValidateAuthorAsync(request.AuthorId);
        if (!validation.IsSuccess)
            return validation;

        _mapper.Map(request, newsPost);
        NormalizeNewsPost(newsPost);
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
