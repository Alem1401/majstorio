using Majstorio.Model.Responses.Appointment;
using Majstorio.Services.Database;
using Mapster;

namespace Majstorio.Services.Mapping;

public class AppointmentMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Appointments are created and moved only by the offer and status actions, so there are no request mappings.
        config.NewConfig<Appointment, AppointmentResponse>();
    }
}
