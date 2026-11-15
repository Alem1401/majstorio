using Majstorio.Model.Requests.ServiceCategory;
using Majstorio.Model.Responses.ServiceCategory;
using Majstorio.Services.Database;
using Mapster;

namespace Majstorio.Services.Mapping;

public class ServiceCategoryMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ServiceCategory, ServiceCategoryResponse>();
        config.NewConfig<ServiceCategoryUpdateRequest, ServiceCategory>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.CreatedAtUtc);
    }
}
