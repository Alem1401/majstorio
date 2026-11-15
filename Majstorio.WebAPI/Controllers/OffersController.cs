using Majstorio.Model.Requests.Offer;
using Majstorio.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// TEMPORARY: open while the desktop app has no login screen yet.
// Will be replaced with [Authorize] plus role and ownership checks.
[AllowAnonymous]
public class OffersController : ControllerBase
{
    private readonly IOfferService _offerService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public OffersController(IOfferService offerService, ResultToStatusCodeMapper resultMapper)
    {
        _offerService = offerService;
        _resultMapper = resultMapper;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] OfferSearchRequest search)
        => _resultMapper.Map(await _offerService.GetOffersAsync(search));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => _resultMapper.Map(await _offerService.GetOfferByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OfferCreateRequest request)
        => _resultMapper.Map(await _offerService.CreateOfferAsync(request));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] OfferUpdateRequest request)
    {
        request.Id = id;
        return _resultMapper.Map(await _offerService.UpdateOfferAsync(request));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => _resultMapper.Map(await _offerService.DeleteOfferAsync(new OfferDeleteRequest { Id = id }));
}
