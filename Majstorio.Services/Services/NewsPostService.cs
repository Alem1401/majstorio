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
        if (!await _ctx.ApplicationUsers.AnyAsync(user => user.Id == request.AuthorId))
            return Result.Failure(ErrorType.NotFound, "Author not found.");

        await _ctx.NewsPosts.AddAsync(_mapper.Map<NewsPost>(request));
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
        var newsPosts = await _ctx.NewsPosts.ToListAsync();
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

        if (!await _ctx.ApplicationUsers.AnyAsync(user => user.Id == request.AuthorId))
            return Result.Failure(ErrorType.NotFound, "Author not found.");

        _mapper.Map(request, newsPost);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }
}
