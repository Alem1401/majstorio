using Majstorio.Model.Requests.NewsPost;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.NewsPost;
using Majstorio.Model.Results;

public interface INewsPostService
{
    Task<Result<PagedResponse<NewsPostResponse>>> GetNewsPostsAsync(NewsPostSearchRequest search);

    Task<Result<NewsPostResponse>> GetNewsPostByIdAsync(int id);

    // The author is the signed-in user, read from the token by the controller.
    Task<Result<NewsPostResponse>> CreateNewsPostAsync(int authorId, NewsPostCreateRequest request);

    Task<Result> UpdateNewsPostAsync(int id, NewsPostUpdateRequest request);

    Task<Result> DeleteNewsPostAsync(NewsPostDeleteRequest request);
}
