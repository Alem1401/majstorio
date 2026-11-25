using System.Text.Json.Serialization;

namespace Majstorio.Model.Requests.TradespersonProfile;

public class TradespersonProfileActiveRequest
{
    // Filled in by the controller from the route (PUT /TradespersonProfiles/{id}/active).
    // [JsonIgnore]: an id sent in the body is never read, so the client cannot pick another record.
    [JsonIgnore]
    public int Id { get; set; }
    public bool IsActive { get; set; }
}
