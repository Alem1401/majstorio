using Majstorio.Model.Requests.ServiceCategory;
using Majstorio.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// TEMPORARY: open while the desktop app has no login screen yet.
// Will be replaced with [Authorize(Roles = RoleNames.Administrator)].
[AllowAnonymous]
public class ServiceCategoriesController : ControllerBase
{
    private readonly IServiceCategoryService _serviceCategoryService;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public ServiceCategoriesController(IServiceCategoryService serviceCategoryService, ResultToStatusCodeMapper resultMapper)
    {
        _serviceCategoryService = serviceCategoryService;
        _resultMapper = resultMapper;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] ServiceCategorySearchRequest search)
        => _resultMapper.Map(await _serviceCategoryService.GetServiceCategoriesAsync(search));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => _resultMapper.Map(await _serviceCategoryService.GetServiceCategoryByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ServiceCategoryCreateRequest request)
        => _resultMapper.Map(await _serviceCategoryService.CreateServiceCategoryAsync(request));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ServiceCategoryUpdateRequest request)
        => _resultMapper.Map(await _serviceCategoryService.UpdateServiceCategoryAsync(id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => _resultMapper.Map(await _serviceCategoryService.DeleteServiceCategoryAsync(
            new ServiceCategoryDeleteRequest { Id = id }));
}
