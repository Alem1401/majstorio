using Majstorio.Model.Requests.NewsPost;
using Majstorio.Services.Constants;
using Majstorio.WebAPI.Services;
using Majstorio.WebAPI.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// Every signed-in user can read; only administrators can change.
[Authorize]
public class NewsPostController : ControllerBase
{
    private readonly INewsPostService _newsPostService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public NewsPostController(INewsPostService newsPostService, ResultToStatusCodeMapper resultMapper)
    {
        _newsPostService = newsPostService;
        _resultMapper = resultMapper;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] NewsPostSearchRequest search)
        => _resultMapper.Map(await _newsPostService.GetNewsPostsAsync(search));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => _resultMapper.Map(await _newsPostService.GetNewsPostByIdAsync(id));

    [Authorize(Roles = RoleNames.Administrator)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NewsPostCreateRequest request)
        => _resultMapper.Map(await _newsPostService.CreateNewsPostAsync(User.ToCurrentUser().Id, request));

    [Authorize(Roles = RoleNames.Administrator)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] NewsPostUpdateRequest request)
        => _resultMapper.Map(await _newsPostService.UpdateNewsPostAsync(id, request));

    [Authorize(Roles = RoleNames.Administrator)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => _resultMapper.Map(await _newsPostService.DeleteNewsPostAsync(new NewsPostDeleteRequest { Id = id }));
}
