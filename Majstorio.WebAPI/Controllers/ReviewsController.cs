using Majstorio.Model.Requests.Review;
using Majstorio.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// TEMPORARY: open while the desktop app has no login screen yet.
// Will be replaced with [Authorize] plus role and ownership checks.
[AllowAnonymous]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public ReviewsController(IReviewService reviewService, ResultToStatusCodeMapper resultMapper)
    {
        _reviewService = reviewService;
        _resultMapper = resultMapper;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] ReviewSearchRequest search)
        => _resultMapper.Map(await _reviewService.GetReviewsAsync(search));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => _resultMapper.Map(await _reviewService.GetReviewByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReviewCreateRequest request)
        => _resultMapper.Map(await _reviewService.CreateReviewAsync(request));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ReviewUpdateRequest request)
    {
        request.Id = id;
        return _resultMapper.Map(await _reviewService.UpdateReviewAsync(request));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => _resultMapper.Map(await _reviewService.DeleteReviewAsync(new ReviewDeleteRequest { Id = id }));
}
