using System.Text.Json.Serialization;

namespace Majstorio.Model.Requests.ServiceRequest;

public class ServiceRequestCancelRequest
{
    // Filled in by the controller from the route (POST /ServiceRequests/{id}/cancel).
    // [JsonIgnore]: an id sent in the body is never read, so the client cannot pick another record.
    [JsonIgnore]
    public int Id { get; set; }
    public string? Reason { get; set; }
}
