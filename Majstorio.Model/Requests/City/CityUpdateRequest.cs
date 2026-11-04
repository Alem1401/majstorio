namespace Majstorio.Model.Requests.City;

// The city id comes from the route (PUT /Cities/{id}), not from the body.
public class CityUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
