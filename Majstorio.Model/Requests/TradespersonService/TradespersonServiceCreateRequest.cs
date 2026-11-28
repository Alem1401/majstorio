namespace Majstorio.Model.Requests.TradespersonService;

public class TradespersonServiceCreateRequest
{
    // The profile the service is added to.
    public int TradespersonProfileId { get; set; }
    public int ServiceCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MinimumPrice { get; set; }
    public decimal MaximumPrice { get; set; }
    public int EstimatedDurationMinutes { get; set; }
}
