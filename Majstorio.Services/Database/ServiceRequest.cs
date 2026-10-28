using Majstorio.Services.Enums;

namespace Majstorio.Services.Database;

// A customer's job request; offers, status history and related notifications refer to it.
public class ServiceRequest : BaseEntity
{
    // Links to ApplicationUser.Id; one customer can create many requests.
    public int CustomerId { get; set; }
    // Links to City.Id; one city can be selected on many requests.
    public int CityId { get; set; }
    // Links to ServiceCategory.Id; one category can be selected on many requests.
    public int ServiceCategoryId { get; set; }
    // Optional profile link; null means an open request rather than a directly targeted one.
    public int? TargetTradespersonProfileId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateTime DesiredStartUtc { get; set; }
    public decimal MaximumBudget { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Created;

    public ApplicationUser Customer { get; set; } = null!;
    public City City { get; set; } = null!;
    public ServiceCategory ServiceCategory { get; set; } = null!;
    public TradespersonProfile? TargetTradespersonProfile { get; set; }
}
