using Majstorio.Model.Responses.Dashboard;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

public class DashboardService : IDashboardService
{
    // The charts show only the busiest categories and cities.
    private const int ChartBars = 5;

    private readonly MajstorioDbContext _ctx;

    public DashboardService(MajstorioDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<Result<DashboardResponse>> GetDashboardAsync()
    {
        var response = new DashboardResponse
        {
            ActiveUserCount = await _ctx.ApplicationUsers.CountAsync(user => user.IsActive),
            // A profile that is active but not approved yet is an application waiting for the administrator.
            PendingTradespersonCount = await _ctx.TradespersonProfiles
                .CountAsync(profile => profile.IsActive && !profile.IsApproved),
            ActiveRequestCount = await _ctx.ServiceRequests.CountAsync(request =>
                request.Status != RequestStatus.Completed && request.Status != RequestStatus.Cancelled),
            CompletedServiceCount = await _ctx.Appointments
                .CountAsync(appointment => appointment.Status == AppointmentStatus.Completed),

            // Each chart is one GROUP BY query that returns only its top rows.
            RequestsByCategory = await _ctx.ServiceRequests
                .GroupBy(request => request.ServiceCategory.Name)
                .Select(group => new NameCountResponse { Name = group.Key, Count = group.Count() })
                .OrderByDescending(row => row.Count).ThenBy(row => row.Name)
                .Take(ChartBars)
                .ToListAsync(),
            RequestsByCity = await _ctx.ServiceRequests
                .GroupBy(request => request.City.Name)
                .Select(group => new NameCountResponse { Name = group.Key, Count = group.Count() })
                .OrderByDescending(row => row.Count).ThenBy(row => row.Name)
                .Take(ChartBars)
                .ToListAsync()
        };

        return Result<DashboardResponse>.Success(response);
    }
}
