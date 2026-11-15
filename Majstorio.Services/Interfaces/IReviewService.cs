using Majstorio.Model.Requests.Review;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Review;
using Majstorio.Model.Results;

public interface IReviewService
{
    Task<Result<PagedResponse<ReviewResponse>>> GetReviewsAsync(ReviewSearchRequest search);
    Task<Result<ReviewResponse>> GetReviewByIdAsync(int id);
    Task<Result> CreateReviewAsync(ReviewCreateRequest request);
    Task<Result> UpdateReviewAsync(ReviewUpdateRequest request);
    Task<Result> DeleteReviewAsync(ReviewDeleteRequest request);
}
