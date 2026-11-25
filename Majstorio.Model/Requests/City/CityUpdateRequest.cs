using System.Text.Json.Serialization;

namespace Majstorio.Model.Requests.City;

public class CityUpdateRequest
{
    // Filled in by the controller from the route (PUT /Cities/{id}).
    // [JsonIgnore]: an id sent in the body is never read, so the client cannot pick another record.
    [JsonIgnore]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
