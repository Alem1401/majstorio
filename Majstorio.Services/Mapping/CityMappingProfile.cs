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

        config.NewConfig<CityInsertRequest, City>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.CreatedAtUtc)
            .Ignore(destination => destination.IsActive);

        config.NewConfig<CityUpdateRequest, City>()
            .Map(destination => destination.IsActive, source => source.isActive)
            .Ignore(destination => destination.CreatedAtUtc);

        config.NewConfig<CityDeleteRequest, City>()
            .Ignore(destination => destination.Name)
            .Ignore(destination => destination.IsActive)
            .Ignore(destination => destination.CreatedAtUtc);
    }
}
