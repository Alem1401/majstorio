namespace Majstorio.Model.Responses.Dashboard;

// The numbers and the two small charts on the administrator's home screen.
public class DashboardResponse
{
    public int ActiveUserCount { get; set; }
    public int PendingTradespersonCount { get; set; }
    public int ActiveRequestCount { get; set; }
    public int CompletedServiceCount { get; set; }
    public List<NameCountResponse> RequestsByCategory { get; set; } = [];
    public List<NameCountResponse> RequestsByCity { get; set; } = [];
}

// One bar of a chart: a name and how many service requests it has.
public class NameCountResponse
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
