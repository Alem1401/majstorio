using Majstorio.Model.Requests.Review;
using Majstorio.Services.Constants;
using Majstorio.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// Review moderation from the desktop app: list and delete only. Customers rate jobs through MyReviewsController.
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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => _resultMapper.Map(await _reviewService.DeleteReviewAsync(new ReviewDeleteRequest { Id = id }));
}
