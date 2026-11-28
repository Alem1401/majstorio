using Majstorio.Model.Requests.Offer;
using Majstorio.Services.Constants;
using Majstorio.WebAPI.Services;
using Majstorio.WebAPI.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class OffersController : ControllerBase
{
    private readonly IOfferService _offerService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public OffersController(IOfferService offerService, ResultToStatusCodeMapper resultMapper)
    {
        _offerService = offerService;
        _resultMapper = resultMapper;
    }

    // Administrative overview for the desktop app.
    [Authorize(Roles = RoleNames.Administrator)]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] OfferSearchRequest search)
        => _resultMapper.Map(await _offerService.GetOffersAsync(search));

    // Offers sent by the signed-in tradesperson, or received on the signed-in customer's requests.
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine([FromQuery] OfferSearchRequest search)
        => _resultMapper.Map(await _offerService.GetMyOffersAsync(User.ToCurrentUser(), search));

    [Authorize(Roles = RoleNames.Tradesperson)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OfferCreateRequest request)
        => _resultMapper.Map(await _offerService.CreateOfferAsync(User.ToCurrentUser(), request));

    [Authorize(Roles = RoleNames.Tradesperson)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] OfferUpdateRequest request)
    {
        request.Id = id;
        return _resultMapper.Map(await _offerService.UpdateOfferAsync(User.ToCurrentUser(), request));
    }

    [Authorize(Roles = RoleNames.Tradesperson)]
    [HttpPost("{id:int}/withdraw")]
    public async Task<IActionResult> Withdraw(int id)
        => _resultMapper.Map(await _offerService.WithdrawOfferAsync(User.ToCurrentUser(), id));

    // Only the customer who owns the request can accept; the service checks that.
    [HttpPost("{id:int}/accept")]
    public async Task<IActionResult> Accept(int id)
        => _resultMapper.Map(await _offerService.AcceptOfferAsync(User.ToCurrentUser(), id));
}
