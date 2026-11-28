using Majstorio.Model.Requests.TradespersonService;
using Majstorio.Services.Constants;
using Majstorio.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// Customers see the active services through the profile detail; this is the administration.
[Authorize(Roles = RoleNames.Administrator)]
public class TradespersonServicesController : ControllerBase
{
    private readonly ITradespersonServiceService _tradespersonServiceService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public TradespersonServicesController(
        ITradespersonServiceService tradespersonServiceService, ResultToStatusCodeMapper resultMapper)
    {
        _tradespersonServiceService = tradespersonServiceService;
        _resultMapper = resultMapper;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] TradespersonServiceSearchRequest search)
        => _resultMapper.Map(await _tradespersonServiceService.GetServicesAsync(search));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => _resultMapper.Map(await _tradespersonServiceService.GetServiceByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TradespersonServiceCreateRequest request)
        => _resultMapper.Map(await _tradespersonServiceService.CreateServiceAsync(request));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TradespersonServiceUpdateRequest request)
    {
        request.Id = id;
        return _resultMapper.Map(await _tradespersonServiceService.UpdateServiceAsync(request));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => _resultMapper.Map(await _tradespersonServiceService.DeleteServiceAsync(
            new TradespersonServiceDeleteRequest { Id = id }));
}
