using Majstorio.Model.Requests.Offer;
using Majstorio.Model.Responses.Offer;
using Majstorio.Services.Database;
using Mapster;

namespace Majstorio.Services.Mapping;

public class OfferMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Offer, OfferResponse>();

        // The tradesperson and the status are set by the service, never by the client.
        config.NewConfig<OfferCreateRequest, Offer>();
        config.NewConfig<OfferUpdateRequest, Offer>()
            .Ignore(destination => destination.Id);
    }
}
