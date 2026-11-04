using Majstorio.Model.Requests.City;
using Majstorio.Model.Responses.City;
using Majstorio.Services.Database;
using Mapster;

namespace Majstorio.Services.Mapping;

public class CityMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<City, CityResponse>();

        config.NewConfig<CityUpdateRequest, City>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.CreatedAtUtc);
    }
}
