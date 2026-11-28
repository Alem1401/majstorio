namespace Majstorio.Model.Responses.TradespersonProfile;

public class TradespersonProfileResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public int CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }
    // Null while the tradesperson has no reviews.
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int CompletedJobs { get; set; }
    // Price range in EUR over the active services; null when there are none.
    public decimal? MinimumPrice { get; set; }
    public decimal? MaximumPrice { get; set; }
    public List<string> Categories { get; set; } = [];
    // Why the recommender ranked this profile here, strongest first: "Category", "City", "Rating",
    // "Popularity". Filled only in the list a non-administrator receives.
    public List<string> RecommendationReasons { get; set; } = [];
}
