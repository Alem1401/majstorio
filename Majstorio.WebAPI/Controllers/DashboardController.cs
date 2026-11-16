using Majstorio.Services.Constants;
using Majstorio.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// The dashboard is part of the desktop administration.
[Authorize(Roles = RoleNames.Administrator)]
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
