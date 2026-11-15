using Majstorio.Model.Requests.User;
using Majstorio.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// TEMPORARY: open while the desktop app has no login screen yet.
// Will be replaced with [Authorize(Roles = RoleNames.Administrator)].
[AllowAnonymous]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public UsersController(IUserService userService, ResultToStatusCodeMapper resultMapper)
    {
        _userService = userService;
        _resultMapper = resultMapper;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] UserSearchRequest search)
        => _resultMapper.Map(await _userService.GetUsersAsync(search));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => _resultMapper.Map(await _userService.GetUserByIdAsync(id));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UserUpdateRequest request)
    {
        request.Id = id;
        return _resultMapper.Map(await _userService.UpdateUserAsync(request));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => _resultMapper.Map(await _userService.DeleteUserAsync(new UserDeleteRequest { Id = id }));
}
