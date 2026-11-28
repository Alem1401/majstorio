using Majstorio.Model.Requests.TradespersonService;
using Majstorio.Model.Responses.TradespersonProfile;
using Majstorio.Services.Database;
using Mapster;

namespace Majstorio.Services.Mapping;

public class TradespersonServiceMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<TradespersonService, TradespersonServiceResponse>()
            .Map(destination => destination.ServiceCategoryName, source => source.ServiceCategory.Name);

        config.NewConfig<TradespersonServiceCreateRequest, TradespersonService>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.CreatedAtUtc)
            .Ignore(destination => destination.IsActive)
            .Ignore(destination => destination.TradespersonProfile)
            .Ignore(destination => destination.ServiceCategory);

        config.NewConfig<TradespersonServiceUpdateRequest, TradespersonService>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.CreatedAtUtc)
            .Ignore(destination => destination.TradespersonProfileId)
            .Ignore(destination => destination.TradespersonProfile)
            .Ignore(destination => destination.ServiceCategory);
    }
}
