using Majstorio.Model.Requests.City;
using Majstorio.Model.Responses.City;

public interface ICityService
{
    Task<bool> CreateCityAsync(CityInsertRequest request);

    Task<bool> UpdateCityAsync(CityUpdateRequest request);

    Task<bool> DeleteCityAsync(CityDeleteRequest request);

    Task<CityResponse?> GetCityByIdAsync(int id);

    Task<List<CityResponse>> GetAllCitiesAsync();
}