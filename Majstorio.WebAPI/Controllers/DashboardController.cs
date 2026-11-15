using Majstorio.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// TEMPORARY: open while the desktop app has no login screen yet.
// Will be replaced with [Authorize(Roles = RoleNames.Administrator)].
[AllowAnonymous]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public DashboardController(IDashboardService dashboardService, ResultToStatusCodeMapper resultMapper)
    {
        _dashboardService = dashboardService;
        _resultMapper = resultMapper;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
        => _resultMapper.Map(await _dashboardService.GetDashboardAsync());
}
