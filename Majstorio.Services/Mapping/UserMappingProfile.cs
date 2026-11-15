using Majstorio.Model.Requests.User;
using Majstorio.Model.Responses.User;
using Majstorio.Services.Database;
using Mapster;

namespace Majstorio.Services.Mapping;

public class UserMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ApplicationUser, UserResponse>()
            .Map(destination => destination.Role, source => source.Role.ToString());
        config.NewConfig<UserCreateRequest, ApplicationUser>();
        // The role is checked and applied by the service; the id never changes.
        config.NewConfig<UserUpdateRequest, ApplicationUser>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.Role);
    }
}
