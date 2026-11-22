using Majstorio.Model.Requests.Review;
using Majstorio.Services.Constants;
using Majstorio.WebAPI.Services;
using Majstorio.WebAPI.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("Reviews/mine")]
// Ratings from the mobile app. A customer sees and creates only reviews of their own completed jobs;
// moderation of all reviews stays in ReviewsController, which is for administrators.
[Authorize(Roles = RoleNames.Customer)]
public class MyReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public MyReviewsController(IReviewService reviewService, ResultToStatusCodeMapper resultMapper)
    {
        _reviewService = reviewService;
        _resultMapper = resultMapper;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] ReviewSearchRequest search)
        => _resultMapper.Map(await _reviewService.GetMyReviewsAsync(User.ToCurrentUser(), search));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReviewCreateRequest request)
        => _resultMapper.Map(await _reviewService.CreateMyReviewAsync(User.ToCurrentUser(), request));
}
