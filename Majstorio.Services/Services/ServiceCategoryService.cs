using Majstorio.Model.Requests.ServiceCategory;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.ServiceCategory;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Extensions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

public class ServiceCategoryService : IServiceCategoryService
{
    private const string NotFoundMessage = "Service category not found.";
    private const string DuplicateMessage = "A service category with this name already exists.";

    private readonly MajstorioDbContext _ctx;
    private readonly IMapper _mapper;

    public ServiceCategoryService(MajstorioDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<Result<PagedResponse<ServiceCategoryResponse>>> GetServiceCategoriesAsync(
        ServiceCategorySearchRequest search)
    {
        var query = _ctx.ServiceCategories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            var name = search.Name.Trim();
            query = query.Where(category => category.Name.Contains(name));
        }

        if (search.IsActive.HasValue)
            query = query.Where(category => category.IsActive == search.IsActive.Value);

        var page = await query
            .OrderBy(category => category.Name)
            .ToPagedAsync(search, categories => _mapper.Map<List<ServiceCategoryResponse>>(categories));

        return Result<PagedResponse<ServiceCategoryResponse>>.Success(page);
    }

    public async Task<Result<ServiceCategoryResponse>> GetServiceCategoryByIdAsync(int id)
    {
        var category = await _ctx.ServiceCategories.AsNoTracking()
            .FirstOrDefaultAsync(category => category.Id == id);
        if (category == null)
            return Result<ServiceCategoryResponse>.Failure(ErrorType.NotFound, NotFoundMessage);

        return Result<ServiceCategoryResponse>.Success(_mapper.Map<ServiceCategoryResponse>(category));
    }

    public async Task<Result<ServiceCategoryResponse>> CreateServiceCategoryAsync(ServiceCategoryCreateRequest request)
    {
        var name = request.Name.Trim();
        if (await _ctx.ServiceCategories.AnyAsync(category => category.Name == name))
            return Result<ServiceCategoryResponse>.Failure(ErrorType.Conflict, DuplicateMessage);

        var category = new ServiceCategory { Name = name };
        _ctx.ServiceCategories.Add(category);
        await _ctx.SaveChangesAsync();
        return Result<ServiceCategoryResponse>.Success(_mapper.Map<ServiceCategoryResponse>(category));
    }

    public async Task<Result> UpdateServiceCategoryAsync(ServiceCategoryUpdateRequest request)
    {
        var category = await _ctx.ServiceCategories.FirstOrDefaultAsync(category => category.Id == request.Id);
        if (category == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        var name = request.Name.Trim();
        if (await _ctx.ServiceCategories.AnyAsync(other => other.Name == name && other.Id != request.Id))
            return Result.Failure(ErrorType.Conflict, DuplicateMessage);

        _mapper.Map(request, category);
        category.Name = name;
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteServiceCategoryAsync(ServiceCategoryDeleteRequest request)
    {
        var category = await _ctx.ServiceCategories.FirstOrDefaultAsync(category => category.Id == request.Id);
        if (category == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        // Categories that records still point to are deactivated instead, so existing data stays intact.
        var isUsed = await _ctx.TradespersonServices.AnyAsync(service => service.ServiceCategoryId == category.Id)
            || await _ctx.ServiceRequests.AnyAsync(serviceRequest => serviceRequest.ServiceCategoryId == category.Id)
            || await _ctx.SearchLogs.AnyAsync(log => log.ServiceCategoryId == category.Id);
        if (isUsed)
            return Result.Failure(ErrorType.Conflict,
                "This category is used by tradespeople, service requests or saved searches, so it cannot be deleted. Deactivate it instead.");

        _ctx.ServiceCategories.Remove(category);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }
}
