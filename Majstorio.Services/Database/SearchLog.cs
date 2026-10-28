namespace Majstorio.Services.Database;

// Saves an actual user's search, with optional city and category filters.
public class SearchLog : BaseEntity
{
    // Links to ApplicationUser.Id; one account can have many saved searches.
    public int UserId { get; set; }
    // Optional City.Id filter; one city can appear in many searches.
    public int? CityId { get; set; }
    // Optional ServiceCategory.Id filter; one category can appear in many searches.
    public int? ServiceCategoryId { get; set; }
    public string? Query { get; set; }
    // Preserve the actual optional rating filter submitted by the user.
    public decimal? MinimumRating { get; set; }
    // Set both bounds when filtering for an available appointment interval.
    public DateTime? AvailableFromUtc { get; set; }
    // The requested interval must end after AvailableFromUtc.
    public DateTime? AvailableToUtc { get; set; }
    public DateTime SearchedAtUtc { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public City? City { get; set; }
    public ServiceCategory? ServiceCategory { get; set; }
}
