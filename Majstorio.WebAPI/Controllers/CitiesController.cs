using Majstorio.Model.Requests.City;
using Majstorio.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// TEMPORARY: open while the desktop app has no login screen yet.
// Will be replaced with [Authorize(Roles = RoleNames.Administrator)].
[AllowAnonymous]
public class CitiesController : ControllerBase
{
    private readonly ICityService _cityService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public CitiesController(ICityService cityService, ResultToStatusCodeMapper resultMapper)
    {
        _cityService = cityService;
        _resultMapper = resultMapper;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] CitySearchRequest search)
        => _resultMapper.Map(await _cityService.GetCitiesAsync(search));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => _resultMapper.Map(await _cityService.GetCityByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CityInsertRequest request)
        => _resultMapper.Map(await _cityService.CreateCityAsync(request));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CityUpdateRequest request)
        => _resultMapper.Map(await _cityService.UpdateCityAsync(id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => _resultMapper.Map(await _cityService.DeleteCityAsync(new CityDeleteRequest { Id = id }));
}
