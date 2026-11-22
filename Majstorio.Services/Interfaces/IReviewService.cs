using Majstorio.Model.Requests.Review;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Review;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;

public interface IReviewService
{
    Task<Result<PagedResponse<ReviewResponse>>> GetReviewsAsync(ReviewSearchRequest search);
    Task<Result<ReviewResponse>> GetReviewByIdAsync(int id);
    Task<Result> CreateReviewAsync(ReviewCreateRequest request);
    Task<Result> UpdateReviewAsync(ReviewUpdateRequest request);
    Task<Result> DeleteReviewAsync(ReviewDeleteRequest request);

    // The signed-in customer's own reviews.
    Task<Result<PagedResponse<ReviewResponse>>> GetMyReviewsAsync(CurrentUser user, ReviewSearchRequest search);
    // A customer rates a completed job on one of their own requests, once.
    Task<Result> CreateMyReviewAsync(CurrentUser user, ReviewCreateRequest request);
}
