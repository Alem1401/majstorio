namespace Majstorio.Model.Requests.City;

public class CitySearchRequest : BaseSearchRequest
{
    // Matches cities whose name contains this text.
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
}
