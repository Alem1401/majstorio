using Majstorio.Model.Requests.Review;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Mapping;
using Majstorio.Services.Services;
using Majstorio.Services.Validators.Review;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

public class ReviewServiceTests : IDisposable
{
    private readonly MajstorioDbContext _ctx;
    private readonly ReviewService _service;

    public ReviewServiceTests()
    {
        _ctx = new MajstorioDbContext(new DbContextOptionsBuilder<MajstorioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = new TypeAdapterConfig();
        new ReviewMappingProfile().Register(config);
        _service = new ReviewService(_ctx, new Mapper(config));
    }

    [Fact]
    public async Task CreateListDelete_PersistsReviewAndMapsResponse()
    {
        var appointment = await AddAppointmentAsync();
        Assert.True((await _service.CreateMyReviewAsync(CustomerOf(appointment),
            new() { AppointmentId = appointment.Id, Rating = 5, Comment = "  Good work.  " })).IsSuccess);
        var review = await _ctx.Reviews.SingleAsync();

        var listed = Assert.Single((await _service.GetReviewsAsync(new())).Value!.Items);
        Assert.Equal(appointment.Id, listed.AppointmentId);
        Assert.Equal(5, listed.Rating);
        Assert.Equal("Good work.", listed.Comment);

        Assert.True((await _service.DeleteReviewAsync(new() { Id = review.Id })).IsSuccess);
        Assert.Empty((await _service.GetReviewsAsync(new())).Value!.Items);
    }

    [Fact]
    public async Task Create_AppointmentOfAnotherCustomer_ReturnsNotFound()
    {
        var appointment = await AddAppointmentAsync();

        var result = await _service.CreateMyReviewAsync(TestSupport.As(NewUser()),
            new() { AppointmentId = appointment.Id, Rating = 5 });

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Empty(await _ctx.Reviews.ToListAsync());
    }

    [Fact]
    public async Task Create_MissingAppointment_ReturnsNotFound()
    {
        var result = await _service.CreateMyReviewAsync(TestSupport.As(NewUser()), new() { AppointmentId = 999, Rating = 5 });

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Empty(await _ctx.Reviews.ToListAsync());
    }

    [Theory]
    [InlineData(AppointmentStatus.Pending)]
    [InlineData(AppointmentStatus.Confirmed)]
    [InlineData(AppointmentStatus.Cancelled)]
    public async Task Create_UnfinishedAppointment_ReturnsConflict(AppointmentStatus status)
    {
        var appointment = await AddAppointmentAsync(status);

        var result = await _service.CreateMyReviewAsync(CustomerOf(appointment),
            new() { AppointmentId = appointment.Id, Rating = 5 });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Empty(await _ctx.Reviews.ToListAsync());
    }

    [Fact]
    public async Task Create_DuplicateReview_ReturnsConflict()
    {
        var appointment = await AddAppointmentAsync();
        var request = new ReviewCreateRequest { AppointmentId = appointment.Id, Rating = 5 };
        Assert.True((await _service.CreateMyReviewAsync(CustomerOf(appointment), request)).IsSuccess);

        Assert.Equal(ErrorType.Conflict, (await _service.CreateMyReviewAsync(CustomerOf(appointment), request)).ErrorType);
        Assert.Single(await _ctx.Reviews.ToListAsync());
    }

    [Fact]
    public async Task MissingReview_ReturnsNotFoundForDelete()
    {
        Assert.Equal(ErrorType.NotFound, (await _service.DeleteReviewAsync(new() { Id = 999 })).ErrorType);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void Validator_RequiresRatingBetweenOneAndFive(int rating, bool isValid)
    {
        var create = new ReviewCreateRequest { AppointmentId = 1, Rating = rating };

        Assert.Equal(isValid, new ReviewCreateRequestValidator().Validate(create).IsValid);
    }

    [Fact]
    public async Task Create_CopiesTradespersonAndCustomerFromTheAppointment()
    {
        var appointment = await AddAppointmentAsync();

        Assert.True((await _service.CreateMyReviewAsync(CustomerOf(appointment),
            new() { AppointmentId = appointment.Id, Rating = 5 })).IsSuccess);

        var review = await _ctx.Reviews.SingleAsync();
        Assert.Equal(appointment.Offer.TradespersonProfileId, review.TradespersonProfileId);
        Assert.Equal(appointment.Offer.ServiceRequest.CustomerId, review.CustomerId);
        var response = Assert.Single((await _service.GetReviewsAsync(new())).Value!.Items);
        Assert.Equal(review.TradespersonProfileId, response.TradespersonProfileId);
        Assert.Equal(review.CustomerId, response.CustomerId);
    }

    private async Task<Appointment> AddAppointmentAsync(AppointmentStatus status = AppointmentStatus.Completed)
    {
        var city = new City { Name = Guid.NewGuid().ToString() };
        var appointment = new Appointment
        {
            Status = status,
            Offer = new Offer
            {
                TradespersonProfile = new TradespersonProfile { User = NewUser(), City = city },
                ServiceRequest = new ServiceRequest
                {
                    Customer = NewUser(), City = city,
                    ServiceCategory = new ServiceCategory { Name = Guid.NewGuid().ToString() }
                }
            }
        };
        _ctx.Appointments.Add(appointment);
        await _ctx.SaveChangesAsync();
        return appointment;
    }

    private static CurrentUser CustomerOf(Appointment appointment) => TestSupport.As(appointment.Offer.ServiceRequest.Customer);

    private static ApplicationUser NewUser() => new()
    {
        UserName = Guid.NewGuid().ToString(), PasswordHash = "test-hash", PasswordSalt = "test-salt"
    };

    public void Dispose() => _ctx.Dispose();
}
