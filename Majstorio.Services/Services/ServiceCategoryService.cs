using Majstorio.Model.Requests.ServiceCategory;
using Majstorio.Model.Responses.ServiceCategory;
using Majstorio.Model.Results;
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

    public async Task<Result> CreateServiceCategoryAsync(ServiceCategoryCreateRequest request)
    {
        if (await _ctx.ServiceCategories.AnyAsync(category => category.Name == request.Name))
            return Result.Failure(ErrorType.Conflict, "Service category already exists.");

        await _ctx.ServiceCategories.AddAsync(_mapper.Map<ServiceCategory>(request));
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteServiceCategoryAsync(ServiceCategoryDeleteRequest request)
    {
        var category = await _ctx.ServiceCategories.FirstOrDefaultAsync(category => category.Id == request.Id);
        if (category == null)
            return Result.Failure(ErrorType.NotFound, "Service category not found.");

        _ctx.ServiceCategories.Remove(category);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<ServiceCategoryResponse>>> GetAllServiceCategoriesAsync()
    {
        var categories = await _ctx.ServiceCategories.ToListAsync();
        return Result<List<ServiceCategoryResponse>>.Success(
            _mapper.Map<List<ServiceCategoryResponse>>(categories));
    }

    public async Task<Result<ServiceCategoryResponse>> GetServiceCategoryByIdAsync(int id)
    {
        var category = await _ctx.ServiceCategories.FirstOrDefaultAsync(category => category.Id == id);
        if (category == null)
            return Result<ServiceCategoryResponse>.Failure(ErrorType.NotFound, "Service category not found.");

        return Result<ServiceCategoryResponse>.Success(_mapper.Map<ServiceCategoryResponse>(category));
    }

    public async Task<Result> UpdateServiceCategoryAsync(ServiceCategoryUpdateRequest request)
    {
        var category = await _ctx.ServiceCategories.FirstOrDefaultAsync(category => category.Id == request.Id);
        if (category == null)
            return Result.Failure(ErrorType.NotFound, "Service category not found.");

        if (await _ctx.ServiceCategories.AnyAsync(other => other.Name == request.Name && other.Id != request.Id))
            return Result.Failure(ErrorType.Conflict, "Service category already exists.");

        _mapper.Map(request, category);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }
}
