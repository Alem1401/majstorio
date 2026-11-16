using Majstorio.Model.Requests.ServiceRequest;
using Majstorio.Model.Responses.ServiceRequest;
using Majstorio.Services.Database;
using Mapster;

namespace Majstorio.Services.Mapping;

public class ServiceRequestMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ServiceRequest, ServiceRequestResponse>();
        // Offers, the appointment and the history are filled by the service, which knows what the caller may see.
        config.NewConfig<ServiceRequest, ServiceRequestDetailResponse>()
            .Ignore(destination => destination.Offers)
            .Ignore(destination => destination.StatusHistory);
        config.NewConfig<StatusHistory, StatusHistoryResponse>();

        // The customer and the status are set by the service, never by the client.
        config.NewConfig<ServiceRequestCreateRequest, ServiceRequest>();
        config.NewConfig<ServiceRequestUpdateRequest, ServiceRequest>()
            .Ignore(destination => destination.Id);
    }
}
