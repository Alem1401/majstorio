using Majstorio.Model.Requests.TradespersonService;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.TradespersonProfile;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Extensions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

// The services a tradesperson offers; administrators manage them.
public class TradespersonServiceService : ITradespersonServiceService
{
    private const string NotFoundMessage = "Service not found.";
    private const string ProfileNotFoundMessage = "Tradesperson profile not found.";
    private const string CategoryNotFoundMessage = "Service category not found or not active.";
    private const string DuplicateMessage = "This tradesperson already has a service with this name.";

    private readonly MajstorioDbContext _ctx;
    private readonly IMapper _mapper;

    public TradespersonServiceService(MajstorioDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<Result<PagedResponse<TradespersonServiceResponse>>> GetServicesAsync(
        TradespersonServiceSearchRequest search)
    {
        IQueryable<TradespersonService> query = _ctx.TradespersonServices.AsNoTracking()
            .Include(service => service.ServiceCategory);

        if (search.TradespersonProfileId.HasValue)
            query = query.Where(service => service.TradespersonProfileId == search.TradespersonProfileId.Value);

        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            var name = search.Name.Trim();
            query = query.Where(service => service.Name.Contains(name));
        }

        if (search.IsActive.HasValue)
            query = query.Where(service => service.IsActive == search.IsActive.Value);

        var page = await query
            .OrderBy(service => service.Name)
            .ThenBy(service => service.Id)
            .ToPagedAsync(search, services => _mapper.Map<List<TradespersonServiceResponse>>(services));

        return Result<PagedResponse<TradespersonServiceResponse>>.Success(page);
    }

    public async Task<Result<TradespersonServiceResponse>> GetServiceByIdAsync(int id)
    {
        var service = await _ctx.TradespersonServices.AsNoTracking()
            .Include(service => service.ServiceCategory)
            .FirstOrDefaultAsync(service => service.Id == id);
        if (service == null)
            return Result<TradespersonServiceResponse>.Failure(ErrorType.NotFound, NotFoundMessage);

        return Result<TradespersonServiceResponse>.Success(_mapper.Map<TradespersonServiceResponse>(service));
    }

    public async Task<Result<TradespersonServiceResponse>> CreateServiceAsync(TradespersonServiceCreateRequest request)
    {
        if (!await _ctx.TradespersonProfiles.AnyAsync(profile => profile.Id == request.TradespersonProfileId))
            return Result<TradespersonServiceResponse>.Failure(ErrorType.NotFound, ProfileNotFoundMessage);

        var category = await _ctx.ServiceCategories
            .FirstOrDefaultAsync(category => category.Id == request.ServiceCategoryId && category.IsActive);
        if (category == null)
            return Result<TradespersonServiceResponse>.Failure(ErrorType.NotFound, CategoryNotFoundMessage);

        var name = request.Name.Trim();
        if (await _ctx.TradespersonServices.AnyAsync(service =>
                service.TradespersonProfileId == request.TradespersonProfileId && service.Name == name))
            return Result<TradespersonServiceResponse>.Failure(ErrorType.Conflict, DuplicateMessage);

        var service = _mapper.Map<TradespersonService>(request);
        service.Name = name;
        service.Description = request.Description.Trim();
        service.ServiceCategory = category;
        _ctx.TradespersonServices.Add(service);
        await _ctx.SaveChangesAsync();
        return Result<TradespersonServiceResponse>.Success(_mapper.Map<TradespersonServiceResponse>(service));
    }

    public async Task<Result> UpdateServiceAsync(TradespersonServiceUpdateRequest request)
    {
        var service = await _ctx.TradespersonServices.FirstOrDefaultAsync(service => service.Id == request.Id);
        if (service == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        // A deactivated category stays on the services that already have it, but cannot be newly chosen.
        if (request.ServiceCategoryId != service.ServiceCategoryId
            && !await _ctx.ServiceCategories.AnyAsync(category =>
                category.Id == request.ServiceCategoryId && category.IsActive))
            return Result.Failure(ErrorType.NotFound, CategoryNotFoundMessage);

        var name = request.Name.Trim();
        if (await _ctx.TradespersonServices.AnyAsync(other => other.Id != request.Id
                && other.TradespersonProfileId == service.TradespersonProfileId && other.Name == name))
            return Result.Failure(ErrorType.Conflict, DuplicateMessage);

        _mapper.Map(request, service);
        service.Name = name;
        service.Description = request.Description.Trim();
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    // No other record points to a service, so it can be deleted.
    public async Task<Result> DeleteServiceAsync(TradespersonServiceDeleteRequest request)
    {
        var service = await _ctx.TradespersonServices.FirstOrDefaultAsync(service => service.Id == request.Id);
        if (service == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        _ctx.TradespersonServices.Remove(service);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }
}
