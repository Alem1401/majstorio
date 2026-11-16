namespace Majstorio.Model.Requests.TradespersonProfile;

public class TradespersonProfileSearchRequest : BaseSearchRequest
{
    // Matches the first or last name.
    public string? Name { get; set; }
    public int? CityId { get; set; }
    public int? ServiceCategoryId { get; set; }
    // 1 to 5; profiles without reviews do not match.
    public decimal? MinimumRating { get; set; }
    // Administrators only; everyone else always gets approved, active profiles.
    public bool? IsApproved { get; set; }
    public bool? IsActive { get; set; }
}
