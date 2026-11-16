using Majstorio.Model.Requests.Report;
using Majstorio.Services.Constants;
using Majstorio.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// Reports are part of the desktop administration.
[Authorize(Roles = RoleNames.Administrator)]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public ReportsController(IReportService reportService, ResultToStatusCodeMapper resultMapper)
    {
        _reportService = reportService;
        _resultMapper = resultMapper;
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests([FromQuery] RequestReportRequest request)
        => _resultMapper.Map(await _reportService.GetRequestReportAsync(request));

    [HttpGet("tradespeople")]
    public async Task<IActionResult> GetTradespeople([FromQuery] TradespersonReportRequest request)
        => _resultMapper.Map(await _reportService.GetTradespersonReportAsync(request));
}
