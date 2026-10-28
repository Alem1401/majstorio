namespace Majstorio.Services.Database;

// A meaningful advertised service with description, price range and estimated duration.
public class TradespersonService : BaseEntity
{
    // Links to TradespersonProfile.Id; one tradesperson can advertise many services.
    public int TradespersonProfileId { get; set; }
    // Links to ServiceCategory.Id; one category can group many advertised services.
    public int ServiceCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MinimumPrice { get; set; }
    public decimal MaximumPrice { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;

    public TradespersonProfile TradespersonProfile { get; set; } = null!;
    public ServiceCategory ServiceCategory { get; set; } = null!;
}
