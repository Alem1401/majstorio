using Majstorio.Model.Requests.City;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.City;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

public class CityService : ICityService
{
    private const string NotFoundMessage = "City not found.";
    private const string DuplicateMessage = "A city with this name already exists.";

    private readonly MajstorioDbContext _ctx;
    private readonly IMapper _mapper;

    public CityService(MajstorioDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<Result<PagedResponse<CityResponse>>> GetCitiesAsync(CitySearchRequest search)
    {
        var query = _ctx.Cities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            var name = search.Name.Trim();
            query = query.Where(city => city.Name.Contains(name));
        }

        if (search.IsActive.HasValue)
            query = query.Where(city => city.IsActive == search.IsActive.Value);

        var totalCount = await query.CountAsync();
        var cities = await query
            .OrderBy(city => city.Name)
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync();

        return Result<PagedResponse<CityResponse>>.Success(new PagedResponse<CityResponse>
        {
            Items = _mapper.Map<List<CityResponse>>(cities),
            TotalCount = totalCount,
            Page = search.Page,
            PageSize = search.PageSize
        });
    }

    public async Task<Result<CityResponse>> GetCityByIdAsync(int id)
    {
        var city = await _ctx.Cities.AsNoTracking().FirstOrDefaultAsync(city => city.Id == id);
        if (city == null)
            return Result<CityResponse>.Failure(ErrorType.NotFound, NotFoundMessage);

        return Result<CityResponse>.Success(_mapper.Map<CityResponse>(city));
    }

    public async Task<Result<CityResponse>> CreateCityAsync(CityInsertRequest request)
    {
        var name = request.Name.Trim();
        if (await _ctx.Cities.AnyAsync(city => city.Name == name))
            return Result<CityResponse>.Failure(ErrorType.Conflict, DuplicateMessage);

        var city = new City { Name = name };
        _ctx.Cities.Add(city);
        await _ctx.SaveChangesAsync();
        return Result<CityResponse>.Success(_mapper.Map<CityResponse>(city));
    }

    public async Task<Result> UpdateCityAsync(int id, CityUpdateRequest request)
    {
        var city = await _ctx.Cities.FirstOrDefaultAsync(city => city.Id == id);
        if (city == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        var name = request.Name.Trim();
        if (await _ctx.Cities.AnyAsync(other => other.Name == name && other.Id != id))
            return Result.Failure(ErrorType.Conflict, DuplicateMessage);

        _mapper.Map(request, city);
        city.Name = name;
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteCityAsync(CityDeleteRequest request)
    {
        var city = await _ctx.Cities.FirstOrDefaultAsync(city => city.Id == request.Id);
        if (city == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        // Cities that records still point to are deactivated instead, so existing data stays intact.
        var isUsed = await _ctx.TradespersonProfiles.AnyAsync(profile => profile.CityId == city.Id)
            || await _ctx.ServiceRequests.AnyAsync(serviceRequest => serviceRequest.CityId == city.Id)
            || await _ctx.SearchLogs.AnyAsync(log => log.CityId == city.Id);
        if (isUsed)
            return Result.Failure(ErrorType.Conflict,
                "This city is used by tradespeople, service requests or saved searches, so it cannot be deleted. Deactivate it instead.");

        _ctx.Cities.Remove(city);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }
}
