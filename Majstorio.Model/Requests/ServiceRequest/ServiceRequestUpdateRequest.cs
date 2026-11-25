using System.Text.Json.Serialization;

namespace Majstorio.Model.Requests.ServiceRequest;

public class ServiceRequestUpdateRequest
{
    // Filled in by the controller from the route (PUT /ServiceRequests/{id}).
    // [JsonIgnore]: an id sent in the body is never read, so the client cannot pick another record.
    [JsonIgnore]
    public int Id { get; set; }
    public int ServiceCategoryId { get; set; }
    public int CityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateTime DesiredStartUtc { get; set; }
    // In EUR.
    public decimal MaximumBudget { get; set; }
}
