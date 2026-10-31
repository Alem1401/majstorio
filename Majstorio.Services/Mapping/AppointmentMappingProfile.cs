using Majstorio.Model.Requests.Appointment;
using Majstorio.Model.Responses.Appointment;
using Majstorio.Services.Database;
using Mapster;

namespace Majstorio.Services.Mapping;

public class AppointmentMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Appointment, AppointmentResponse>();

        config.NewConfig<AppointmentCreateRequest, Appointment>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.CreatedAtUtc)
            .Ignore(destination => destination.Status)
            .Ignore(destination => destination.Offer);

        config.NewConfig<AppointmentUpdateRequest, Appointment>()
            .Ignore(destination => destination.CreatedAtUtc)
            .Ignore(destination => destination.Offer);

        config.NewConfig<AppointmentDeleteRequest, Appointment>()
            .Ignore(destination => destination.OfferId)
            .Ignore(destination => destination.StartUtc)
            .Ignore(destination => destination.EndUtc)
            .Ignore(destination => destination.Status)
            .Ignore(destination => destination.CreatedAtUtc)
            .Ignore(destination => destination.Offer);
    }
}
