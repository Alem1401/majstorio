using Majstorio.Model.Requests.City;
using Majstorio.Services.Constants;
using Majstorio.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// Every signed-in user can read; only administrators can change.
[Authorize]
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

    [Authorize(Roles = RoleNames.Administrator)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CityInsertRequest request)
        => _resultMapper.Map(await _cityService.CreateCityAsync(request));

    [Authorize(Roles = RoleNames.Administrator)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CityUpdateRequest request)
        => _resultMapper.Map(await _cityService.UpdateCityAsync(id, request));

    [Authorize(Roles = RoleNames.Administrator)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => _resultMapper.Map(await _cityService.DeleteCityAsync(new CityDeleteRequest { Id = id }));
}
