using Majstorio.Model.Requests.Review;
using Majstorio.Model.Responses.Review;
using Majstorio.Model.Results;

public interface IReviewService
{
    Task<Result> CreateReviewAsync(ReviewCreateRequest request);
    Task<Result> UpdateReviewAsync(ReviewUpdateRequest request);
    Task<Result> DeleteReviewAsync(ReviewDeleteRequest request);
    Task<Result<List<ReviewResponse>>> GetAllReviewsAsync();
    Task<Result<ReviewResponse>> GetReviewByIdAsync(int id);
}
