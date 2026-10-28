using Majstorio.Model.Requests.ServiceCategory;
using Majstorio.Model.Responses.ServiceCategory;
using Majstorio.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

public class ServiceCategoryService : IServiceCategoryService
{
    private readonly MajstorioDbContext _ctx;
    private readonly IMapper _mapper;

    public ServiceCategoryService(MajstorioDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<bool> CreateServiceCategoryAsync(ServiceCategoryCreateRequest request)
    {
        if (await _ctx.ServiceCategories.AnyAsync(category => category.Name == request.Name))
            return false;

        await _ctx.ServiceCategories.AddAsync(_mapper.Map<ServiceCategory>(request));
        await _ctx.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteServiceCategoryAsync(ServiceCategoryDeleteRequest request)
    {
        var category = await _ctx.ServiceCategories.FirstOrDefaultAsync(category => category.Id == request.Id);
        if (category == null) return false;

        _ctx.ServiceCategories.Remove(category);
        await _ctx.SaveChangesAsync();
        return true;
    }

    public async Task<List<ServiceCategoryResponse>> GetAllServiceCategoriesAsync()
    {
        var categories = await _ctx.ServiceCategories.ToListAsync();
        return _mapper.Map<List<ServiceCategoryResponse>>(categories);
    }

    public async Task<ServiceCategoryResponse?> GetServiceCategoryByIdAsync(int id)
    {
        var category = await _ctx.ServiceCategories.FirstOrDefaultAsync(category => category.Id == id);
        return category == null ? null : _mapper.Map<ServiceCategoryResponse>(category);
    }

    public async Task<bool> UpdateServiceCategoryAsync(ServiceCategoryUpdateRequest request)
    {
        var category = await _ctx.ServiceCategories.FirstOrDefaultAsync(category => category.Id == request.Id);
        if (category == null) return false;

        _mapper.Map(request, category);
        await _ctx.SaveChangesAsync();
        return true;
    }
}
