using Majstorio.Model.Requests.City;
using Majstorio.Model.Responses.City;
using Majstorio.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services
{
    public class CityService : ICityService
    {
        MajstorioDbContext _ctx;
        IMapper _mapper;
        public CityService(MajstorioDbContext ctx, IMapper mapper) { _ctx = ctx;
        _mapper = mapper;
        }

        public async Task<bool> CreateCityAsync(CityInsertRequest request)
        {
            var exists =await _ctx.Set<City>().FirstOrDefaultAsync(c => c.Name == request.Name);
            if (exists != null) { return false; }
            else
            {
                var add = await _ctx.Cities.AddAsync(new City { Name = request.Name });
                if (add != null)
                {
                    await _ctx.SaveChangesAsync();
                    return true;
                }
                else return false;
            }
        }

        public async Task<bool> DeleteCityAsync(CityDeleteRequest request)
        {
            var city =await _ctx.Cities.FirstOrDefaultAsync(c => c.Id == request.Id);
            if(city == null)
            {
                return false;
            }
            else
            {
                var deleted = _ctx.Cities.Remove(city);
                await _ctx.SaveChangesAsync();
                return true;


            }
        }

        public async Task<List<CityResponse>> GetAllCitiesAsync()
        {
            var cities =await _ctx.Cities.ToListAsync();
            return  _mapper.Map<List<CityResponse>>(cities);
        }

        public async Task<CityResponse?> GetCityByIdAsync(int id)
        {
            return _mapper.Map<CityResponse>(await _ctx.Cities.FirstOrDefaultAsync(c => c.Id == id));
        }

        public async Task<bool> UpdateCityAsync(CityUpdateRequest request)
        {
           var toUpdate = await _ctx.Cities.FirstOrDefaultAsync(_ctx => _ctx.Id == request.Id);
            if (toUpdate == null) return false;
            toUpdate = _mapper.Map<City>(request);
            _ctx.Update(toUpdate);
            await _ctx.SaveChangesAsync();
            return true;

        }
    }
}
