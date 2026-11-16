namespace Majstorio.Model.Responses.ServiceRequest;

public class ServiceRequestResponse
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public int ServiceCategoryId { get; set; }
    public string ServiceCategoryName { get; set; } = string.Empty;
    public int? TargetTradespersonProfileId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateTime DesiredStartUtc { get; set; }
    // In EUR.
    public decimal MaximumBudget { get; set; }
    // 0 = Created, 1 = OfferAccepted, 2 = AppointmentConfirmed, 3 = Completed, 4 = Cancelled.
    public int Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
