using System.Text.Json.Serialization;

namespace Majstorio.Model.Requests.TradespersonService;

public class TradespersonServiceUpdateRequest
{
    // Filled in by the controller from the route (PUT /TradespersonServices/{id}).
    // [JsonIgnore]: an id sent in the body is never read, so the client cannot pick another record.
    [JsonIgnore]
    public int Id { get; set; }
    public int ServiceCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MinimumPrice { get; set; }
    public decimal MaximumPrice { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public bool IsActive { get; set; }
}
