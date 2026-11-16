namespace Majstorio.Model.Requests.ServiceRequest;

public class ServiceRequestCancelRequest
{
    // Set from the route (POST /ServiceRequests/{id}/cancel).
    public int Id { get; set; }
    public string? Reason { get; set; }
}
