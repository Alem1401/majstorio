using Majstorio.Model.Requests.City;
using Majstorio.Model.Responses.City;
using Majstorio.Model.Results;

public interface ICityService
{
    Task<Result> CreateCityAsync(CityInsertRequest request);

    Task<Result> UpdateCityAsync(CityUpdateRequest request);

    Task<Result> DeleteCityAsync(CityDeleteRequest request);

    Task<Result<CityResponse>> GetCityByIdAsync(int id);

    Task<Result<List<CityResponse>>> GetAllCitiesAsync();
}
