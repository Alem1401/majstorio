namespace Majstorio.Model.Requests.ServiceRequest;

// The customer and the initial status are not part of the request: the server takes the customer
// from the token and always starts a request as Created.
public class ServiceRequestCreateRequest
{
    public int ServiceCategoryId { get; set; }
    public int CityId { get; set; }
    // Set when the request is started from a tradesperson's detail screen; null for an open request.
    public int? TargetTradespersonProfileId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateTime DesiredStartUtc { get; set; }
    // In EUR.
    public decimal MaximumBudget { get; set; }
}
