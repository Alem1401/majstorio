using Majstorio.Model.Responses.Review;

namespace Majstorio.Model.Responses.TradespersonProfile;

public class TradespersonProfileDetailResponse : TradespersonProfileResponse
{
    public List<TradespersonServiceResponse> Services { get; set; } = [];
    public List<ReviewResponse> LatestReviews { get; set; } = [];
}
