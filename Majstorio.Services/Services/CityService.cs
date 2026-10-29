using Majstorio.Model.Requests.City;
using Majstorio.Model.Responses.City;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services
{
    public class CityService : ICityService
    {
        private readonly MajstorioDbContext _ctx;
        private readonly IMapper _mapper;
        public CityService(MajstorioDbContext ctx, IMapper mapper)
        {
            _ctx = ctx;
            _mapper = mapper;
        }

        public async Task<Result> CreateCityAsync(CityInsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Result.Failure(ErrorType.Validation, "City name is required.");

            if (await _ctx.Cities.AnyAsync(city => city.Name == request.Name))
                return Result.Failure(ErrorType.Conflict, "City already exists.");

            _ctx.Cities.Add(new City { Name = request.Name });
            await _ctx.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> DeleteCityAsync(CityDeleteRequest request)
        {
            var city = await _ctx.Cities.FirstOrDefaultAsync(city => city.Id == request.Id);
            if (city == null)
                return Result.Failure(ErrorType.NotFound, "City not found.");

            _ctx.Cities.Remove(city);
            await _ctx.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result<List<CityResponse>>> GetAllCitiesAsync()
        {
            var cities = await _ctx.Cities.ToListAsync();
            return Result<List<CityResponse>>.Success(_mapper.Map<List<CityResponse>>(cities));
        }

        public async Task<Result<CityResponse>> GetCityByIdAsync(int id)
        {
            var city = await _ctx.Cities.FirstOrDefaultAsync(city => city.Id == id);
            if (city == null)
                return Result<CityResponse>.Failure(ErrorType.NotFound, "City not found.");

            return Result<CityResponse>.Success(_mapper.Map<CityResponse>(city));
        }

        public async Task<Result> UpdateCityAsync(CityUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Result.Failure(ErrorType.Validation, "City name is required.");

            var city = await _ctx.Cities.FirstOrDefaultAsync(city => city.Id == request.Id);
            if (city == null)
                return Result.Failure(ErrorType.NotFound, "City not found.");

            if (await _ctx.Cities.AnyAsync(other => other.Name == request.Name && other.Id != request.Id))
                return Result.Failure(ErrorType.Conflict, "City already exists.");

            _mapper.Map(request, city);
            await _ctx.SaveChangesAsync();
            return Result.Success();
        }
    }
}
