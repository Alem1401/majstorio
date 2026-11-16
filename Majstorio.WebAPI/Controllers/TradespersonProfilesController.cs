using Majstorio.Model.Requests.TradespersonProfile;
using Majstorio.Services.Constants;
using Majstorio.WebAPI.Services;
using Majstorio.WebAPI.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class TradespersonProfilesController : ControllerBase
{
    private readonly ITradespersonProfileService _profileService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public TradespersonProfilesController(ITradespersonProfileService profileService, ResultToStatusCodeMapper resultMapper)
    {
        _profileService = profileService;
        _resultMapper = resultMapper;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] TradespersonProfileSearchRequest search)
        => _resultMapper.Map(await _profileService.GetProfilesAsync(User.ToCurrentUser(), search));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => _resultMapper.Map(await _profileService.GetProfileByIdAsync(User.ToCurrentUser(), id));

    // Approving the profile is what gives its user the Tradesperson role.
    [Authorize(Roles = RoleNames.Administrator)]
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
        => _resultMapper.Map(await _profileService.ApproveProfileAsync(User.ToCurrentUser(), id));

    [Authorize(Roles = RoleNames.Administrator)]
    [HttpPut("{id:int}/active")]
    public async Task<IActionResult> SetActive(int id, [FromBody] TradespersonProfileActiveRequest request)
    {
        request.Id = id;
        return _resultMapper.Map(await _profileService.SetProfileActiveAsync(request));
    }
}
