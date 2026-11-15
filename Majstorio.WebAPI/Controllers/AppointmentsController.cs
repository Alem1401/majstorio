using Majstorio.Model.Requests.Appointment;
using Majstorio.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// TEMPORARY: open while the desktop app has no login screen yet.
// Will be replaced with [Authorize] plus role and ownership checks.
[AllowAnonymous]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public AppointmentsController(IAppointmentService appointmentService, ResultToStatusCodeMapper resultMapper)
    {
        _appointmentService = appointmentService;
        _resultMapper = resultMapper;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AppointmentSearchRequest search)
        => _resultMapper.Map(await _appointmentService.GetAppointmentsAsync(search));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => _resultMapper.Map(await _appointmentService.GetAppointmentByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AppointmentCreateRequest request)
        => _resultMapper.Map(await _appointmentService.CreateAppointmentAsync(request));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AppointmentUpdateRequest request)
    {
        request.Id = id;
        return _resultMapper.Map(await _appointmentService.UpdateAppointmentAsync(request));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => _resultMapper.Map(await _appointmentService.DeleteAppointmentAsync(new AppointmentDeleteRequest { Id = id }));
}
