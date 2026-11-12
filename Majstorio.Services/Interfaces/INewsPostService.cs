using Majstorio.Model.Requests.NewsPost;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.NewsPost;
using Majstorio.Model.Results;

public interface INewsPostService
{
    Task<Result<PagedResponse<NewsPostResponse>>> GetNewsPostsAsync(NewsPostSearchRequest search);

    Task<Result<NewsPostResponse>> GetNewsPostByIdAsync(int id);

    Task<Result<NewsPostResponse>> CreateNewsPostAsync(NewsPostCreateRequest request);

    Task<Result> UpdateNewsPostAsync(int id, NewsPostUpdateRequest request);

    Task<Result> DeleteNewsPostAsync(NewsPostDeleteRequest request);
}
