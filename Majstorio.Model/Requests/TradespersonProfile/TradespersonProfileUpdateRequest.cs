using System.Text.Json.Serialization;

namespace Majstorio.Model.Requests.TradespersonProfile;

public class TradespersonProfileUpdateRequest
{
    // Filled in by the controller from the route (PUT /TradespersonProfiles/{id}).
    // [JsonIgnore]: an id sent in the body is never read, so the client cannot pick another record.
    [JsonIgnore]
    public int Id { get; set; }
    public int CityId { get; set; }
    public string Description { get; set; } = string.Empty;
}
