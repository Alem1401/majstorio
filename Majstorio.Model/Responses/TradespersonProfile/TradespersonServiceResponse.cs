namespace Majstorio.Model.Responses.TradespersonProfile;

public class TradespersonServiceResponse
{
    public int Id { get; set; }
    public int ServiceCategoryId { get; set; }
    public string ServiceCategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MinimumPrice { get; set; }
    public decimal MaximumPrice { get; set; }
    public int EstimatedDurationMinutes { get; set; }
}
