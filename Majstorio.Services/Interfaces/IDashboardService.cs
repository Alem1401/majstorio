using Majstorio.Model.Responses.Dashboard;
using Majstorio.Model.Results;

public interface IDashboardService
{
    Task<Result<DashboardResponse>> GetDashboardAsync();
}
