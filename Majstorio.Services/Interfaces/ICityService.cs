using Majstorio.Model.Requests.City;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.City;
using Majstorio.Model.Results;

public interface ICityService
{
    Task<Result<PagedResponse<CityResponse>>> GetCitiesAsync(CitySearchRequest search);

    Task<Result<CityResponse>> GetCityByIdAsync(int id);

    Task<Result<CityResponse>> CreateCityAsync(CityInsertRequest request);

    Task<Result> UpdateCityAsync(int id, CityUpdateRequest request);

    Task<Result> DeleteCityAsync(CityDeleteRequest request);
}
