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

        config.NewConfig<OfferCreateRequest, Offer>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.CreatedAtUtc)
            .Ignore(destination => destination.Status)
            .Ignore(nameof(Offer.DecisionReason), nameof(Offer.DecidedByUserId), nameof(Offer.DecidedAtUtc))
            .Ignore(destination => destination.ServiceRequest)
            .Ignore(destination => destination.TradespersonProfile)
            .Ignore(nameof(Offer.DecidedByUser));

        config.NewConfig<OfferUpdateRequest, Offer>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.CreatedAtUtc)
            .Ignore(destination => destination.Status)
            .Ignore(nameof(Offer.DecisionReason), nameof(Offer.DecidedByUserId), nameof(Offer.DecidedAtUtc))
            .Ignore(destination => destination.ServiceRequest)
            .Ignore(destination => destination.TradespersonProfile)
            .Ignore(nameof(Offer.DecidedByUser));

        config.NewConfig<OfferDeleteRequest, Offer>()
            .Ignore(destination => destination.ServiceRequestId)
            .Ignore(destination => destination.TradespersonProfileId)
            .Ignore(destination => destination.Price)
            .Ignore(destination => destination.ProposedStartUtc)
            .Ignore(destination => destination.DurationMinutes)
            .Ignore(destination => destination.Status)
            .Ignore(nameof(Offer.DecisionReason), nameof(Offer.DecidedByUserId), nameof(Offer.DecidedAtUtc))
            .Ignore(destination => destination.CreatedAtUtc)
            .Ignore(destination => destination.ServiceRequest)
            .Ignore(destination => destination.TradespersonProfile)
            .Ignore(nameof(Offer.DecidedByUser));
    }
}
