using Majstorio.Model.Requests.Review;
using Majstorio.Model.Responses.Review;
using Majstorio.Services.Database;
using Mapster;

namespace Majstorio.Services.Mapping;

public class ReviewMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Review, ReviewResponse>();

        // TradespersonProfileId and CustomerId are derived by the service from the appointment.
        config.NewConfig<ReviewCreateRequest, Review>()
            .Ignore(destination => destination.Id)
            .Ignore(destination => destination.CreatedAtUtc)
            .Ignore(destination => destination.TradespersonProfileId)
            .Ignore(destination => destination.CustomerId)
            .Ignore(destination => destination.Appointment)
            .Ignore(destination => destination.TradespersonProfile)
            .Ignore(destination => destination.Customer);

        config.NewConfig<ReviewUpdateRequest, Review>()
            .Ignore(destination => destination.CreatedAtUtc)
            .Ignore(destination => destination.TradespersonProfileId)
            .Ignore(destination => destination.CustomerId)
            .Ignore(destination => destination.Appointment)
            .Ignore(destination => destination.TradespersonProfile)
            .Ignore(destination => destination.Customer);

        config.NewConfig<ReviewDeleteRequest, Review>()
            .Ignore(destination => destination.AppointmentId)
            .Ignore(destination => destination.TradespersonProfileId)
            .Ignore(destination => destination.CustomerId)
            .Ignore(destination => destination.Rating)
            .Ignore(nameof(Review.Comment))
            .Ignore(destination => destination.CreatedAtUtc)
            .Ignore(destination => destination.Appointment)
            .Ignore(destination => destination.TradespersonProfile)
            .Ignore(destination => destination.Customer);
    }
}
