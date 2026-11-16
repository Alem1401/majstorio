namespace Majstorio.Model.Requests.TradespersonProfile;

public class TradespersonProfileActiveRequest
{
    // Set from the route (PUT /TradespersonProfiles/{id}/active).
    public int Id { get; set; }
    public bool IsActive { get; set; }
}
