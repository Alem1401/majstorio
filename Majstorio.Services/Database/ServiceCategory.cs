namespace Majstorio.Services.Database;


public class ServiceCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<TradespersonService> TradespersonServices { get; set; } = new List<TradespersonService>();
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
    public ICollection<SearchLog> SearchLogs { get; set; } = new List<SearchLog>();
}
