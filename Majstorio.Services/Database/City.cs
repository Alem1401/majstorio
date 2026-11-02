namespace Majstorio.Services.Database;


public class City : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<TradespersonProfile> TradespersonProfiles { get; set; } = new List<TradespersonProfile>();
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
    public ICollection<SearchLog> SearchLogs { get; set; } = new List<SearchLog>();
}
