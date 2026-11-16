using Majstorio.Model.Requests.ServiceRequest;
using Majstorio.WebAPI.Services;
using Majstorio.WebAPI.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ServiceRequestsController : ControllerBase
{
    private readonly IServiceRequestService _serviceRequestService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public ServiceRequestsController(IServiceRequestService serviceRequestService, ResultToStatusCodeMapper resultMapper)
    {
        _serviceRequestService = serviceRequestService;
        _resultMapper = resultMapper;
    }

    // What the list contains depends on the role in the token; see IServiceRequestService.
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] ServiceRequestSearchRequest search)
        => _resultMapper.Map(await _serviceRequestService.GetServiceRequestsAsync(User.ToCurrentUser(), search));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => _resultMapper.Map(await _serviceRequestService.GetServiceRequestByIdAsync(User.ToCurrentUser(), id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ServiceRequestCreateRequest request)
        => _resultMapper.Map(await _serviceRequestService.CreateServiceRequestAsync(User.ToCurrentUser(), request));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ServiceRequestUpdateRequest request)
    {
        request.Id = id;
        return _resultMapper.Map(await _serviceRequestService.UpdateServiceRequestAsync(User.ToCurrentUser(), request));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => _resultMapper.Map(await _serviceRequestService.DeleteServiceRequestAsync(
            User.ToCurrentUser(), new ServiceRequestDeleteRequest { Id = id }));

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] ServiceRequestCancelRequest request)
    {
        request.Id = id;
        return _resultMapper.Map(await _serviceRequestService.CancelServiceRequestAsync(User.ToCurrentUser(), request));
    }
}
