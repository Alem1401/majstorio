using Majstorio.Model.Requests.ServiceCategory;
using Majstorio.Services.Constants;
using Majstorio.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// Every signed-in user can read; only administrators can change.
[Authorize]
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

    [Authorize(Roles = RoleNames.Administrator)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ServiceCategoryCreateRequest request)
        => _resultMapper.Map(await _serviceCategoryService.CreateServiceCategoryAsync(request));

    [Authorize(Roles = RoleNames.Administrator)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ServiceCategoryUpdateRequest request)
        => _resultMapper.Map(await _serviceCategoryService.UpdateServiceCategoryAsync(id, request));

    [Authorize(Roles = RoleNames.Administrator)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => _resultMapper.Map(await _serviceCategoryService.DeleteServiceCategoryAsync(
            new ServiceCategoryDeleteRequest { Id = id }));
}
