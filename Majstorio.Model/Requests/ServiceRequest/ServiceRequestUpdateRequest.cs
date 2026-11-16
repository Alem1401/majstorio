namespace Majstorio.Model.Requests.ServiceRequest;

public class ServiceRequestUpdateRequest
{
    // Set from the route (PUT /ServiceRequests/{id}); a value in the body is overwritten.
    public int Id { get; set; }
    public int ServiceCategoryId { get; set; }
    public int CityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateTime DesiredStartUtc { get; set; }
    // In EUR.
    public decimal MaximumBudget { get; set; }
}
