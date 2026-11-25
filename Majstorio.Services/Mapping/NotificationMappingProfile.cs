using Majstorio.Model.Responses.Notification;
using Majstorio.Services.Database;
using Mapster;

namespace Majstorio.Services.Mapping;

public class NotificationMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Notification, NotificationResponse>();
    }
}
