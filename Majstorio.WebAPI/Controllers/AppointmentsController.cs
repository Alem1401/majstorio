using Majstorio.Model.Requests.Appointment;
using Majstorio.Services.Constants;
using Majstorio.WebAPI.Services;
using Majstorio.WebAPI.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public AppointmentsController(IAppointmentService appointmentService, ResultToStatusCodeMapper resultMapper)
    {
        _appointmentService = appointmentService;
        _resultMapper = resultMapper;
    }

    // Administrative overview for the desktop app.
    [Authorize(Roles = RoleNames.Administrator)]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AppointmentSearchRequest search)
        => _resultMapper.Map(await _appointmentService.GetAppointmentsAsync(search));

    [Authorize(Roles = RoleNames.Administrator)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => _resultMapper.Map(await _appointmentService.GetAppointmentByIdAsync(id));

    // Appointments of the signed-in customer's requests, or of the jobs assigned to the signed-in tradesperson.
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine([FromQuery] AppointmentSearchRequest search)
        => _resultMapper.Map(await _appointmentService.GetMyAppointmentsAsync(User.ToCurrentUser(), search));

    [Authorize(Roles = RoleNames.Tradesperson)]
    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
        => _resultMapper.Map(await _appointmentService.CompleteAppointmentAsync(User.ToCurrentUser(), id));
}
