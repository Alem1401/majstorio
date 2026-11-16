using Majstorio.Model.Requests.Review;
using Majstorio.Services.Constants;
using Majstorio.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// Review moderation from the desktop app. Customer reviews from the mobile app will get their own actions with ownership checks.
[Authorize(Roles = RoleNames.Administrator)]
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
