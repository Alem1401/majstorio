using Majstorio.Model.Requests.NewsPost;
using Majstorio.Model.Responses.NewsPost;
using Majstorio.Model.Results;

public interface INewsPostService
{
    Task<Result> CreateNewsPostAsync(NewsPostCreateRequest request);
    Task<Result> UpdateNewsPostAsync(NewsPostUpdateRequest request);
    Task<Result> DeleteNewsPostAsync(NewsPostDeleteRequest request);
    Task<Result<List<NewsPostResponse>>> GetAllNewsPostsAsync();
    Task<Result<NewsPostResponse>> GetNewsPostByIdAsync(int id);
}
