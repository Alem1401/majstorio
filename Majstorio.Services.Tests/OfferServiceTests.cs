using Majstorio.Model.Requests.Offer;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Mapping;
using Majstorio.Services.Services;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

public class OfferServiceTests : IDisposable
{
    private readonly MajstorioDbContext _ctx;
    private readonly OfferService _service;
    private readonly ServiceRequest _request;
    private readonly TradespersonProfile _profile;
    private readonly DateTime _start = DateTime.UtcNow.AddDays(2);

    public OfferServiceTests()
    {
        _ctx = new MajstorioDbContext(new DbContextOptionsBuilder<MajstorioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = new TypeAdapterConfig();
        new OfferMappingProfile().Register(config);
        _service = new OfferService(_ctx, new Mapper(config));
        var city = new City { Name = "Mostar" };
        var category = new ServiceCategory { Name = "Plumbing" };
        _request = new ServiceRequest
        {
            Customer = NewUser(), City = city, ServiceCategory = category, MaximumBudget = 500m
        };
        _profile = new TradespersonProfile { User = NewUser(), City = city, IsApproved = true };
        _ctx.ServiceRequests.Add(_request);
        _ctx.TradespersonServices.Add(new TradespersonService
        {
            TradespersonProfile = _profile, ServiceCategory = category, Name = "Repairs"
        });
        _ctx.SaveChanges();
    }

    [Fact]
    public async Task Crud_MapsFieldsPreservesCreationTimeAndDeletesPendingOffer()
    {
        Assert.True((await _service.CreateOfferAsync(CreateRequest())).IsSuccess);
        var offer = await _ctx.Offers.SingleAsync();
        var createdAt = offer.CreatedAtUtc;
        Assert.Equal(OfferStatus.Pending, offer.Status);
        Assert.Null(offer.DecidedAtUtc);
        Assert.Null(offer.DecidedByUserId);
        var update = UpdateRequest(offer);
        update.Price = 125m;
        update.ProposedStartUtc = _start.AddDays(1);
        update.DurationMinutes = 90;

        Assert.True((await _service.UpdateOfferAsync(update)).IsSuccess);
        _ctx.ChangeTracker.Clear();
        var response = await _service.GetOfferByIdAsync(offer.Id);
        Assert.True(response.IsSuccess);
        Assert.Equal(_request.Id, response.Value!.ServiceRequestId);
        Assert.Equal(_profile.Id, response.Value.TradespersonProfileId);
        Assert.Equal(update.Price, response.Value.Price);
        Assert.Equal(update.ProposedStartUtc, response.Value.ProposedStartUtc);
        Assert.Equal(update.DurationMinutes, response.Value.DurationMinutes);
        Assert.Equal((int)OfferStatus.Pending, response.Value.Status);
        Assert.Equal(createdAt, (await _ctx.Offers.SingleAsync()).CreatedAtUtc);
        Assert.Single((await _service.GetAllOffersAsync()).Value!);
        Assert.True((await _service.DeleteOfferAsync(new() { Id = offer.Id })).IsSuccess);
        Assert.Empty((await _service.GetAllOffersAsync()).Value!);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_MissingRelatedRecord_ReturnsNotFound(bool missingRequest)
    {
        var request = CreateRequest();
        if (missingRequest) request.ServiceRequestId = 999;
        else request.TradespersonProfileId = 999;

        Assert.Equal(ErrorType.NotFound, (await _service.CreateOfferAsync(request)).ErrorType);
        Assert.Empty(await _ctx.Offers.ToListAsync());
    }

    [Theory]
    [InlineData(RequestStatus.OfferAccepted)]
    [InlineData(RequestStatus.AppointmentConfirmed)]
    [InlineData(RequestStatus.Completed)]
    [InlineData(RequestStatus.Cancelled)]
    public async Task Create_RequestNoLongerOpen_ReturnsConflict(RequestStatus status)
    {
        _request.Status = status;
        await _ctx.SaveChangesAsync();

        Assert.Equal(ErrorType.Conflict, (await _service.CreateOfferAsync(CreateRequest())).ErrorType);
    }

    [Theory]
    [InlineData("inactive profile")]
    [InlineData("unapproved profile")]
    [InlineData("inactive account")]
    public async Task Create_IneligibleTradesperson_ReturnsForbidden(string reason)
    {
        if (reason == "inactive profile") _profile.IsActive = false;
        if (reason == "unapproved profile") _profile.IsApproved = false;
        if (reason == "inactive account") _profile.User.IsActive = false;
        await _ctx.SaveChangesAsync();

        Assert.Equal(ErrorType.Forbidden, (await _service.CreateOfferAsync(CreateRequest())).ErrorType);
    }

    [Fact]
    public async Task Create_OwnRequest_ReturnsForbidden()
    {
        _request.CustomerId = _profile.UserId;
        await _ctx.SaveChangesAsync();

        Assert.Equal(ErrorType.Forbidden, (await _service.CreateOfferAsync(CreateRequest())).ErrorType);
    }

    [Fact]
    public async Task Create_RequestTargetsAnotherTradesperson_ReturnsForbidden()
    {
        _request.TargetTradespersonProfileId = 999;
        await _ctx.SaveChangesAsync();

        Assert.Equal(ErrorType.Forbidden, (await _service.CreateOfferAsync(CreateRequest())).ErrorType);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_CityOrActiveCategoryMismatch_ReturnsConflict(bool wrongCity)
    {
        if (wrongCity) _profile.CityId = 999;
        else (await _ctx.TradespersonServices.SingleAsync()).IsActive = false;
        await _ctx.SaveChangesAsync();

        Assert.Equal(ErrorType.Conflict, (await _service.CreateOfferAsync(CreateRequest())).ErrorType);
    }

    [Fact]
    public async Task Create_PriceAboveBudget_ReturnsConflict()
    {
        var request = CreateRequest();
        request.Price = _request.MaximumBudget + 0.01m;

        Assert.Equal(ErrorType.Conflict, (await _service.CreateOfferAsync(request)).ErrorType);
    }

    [Fact]
    public async Task Create_PriceAtBudget_IsAllowed()
    {
        var request = CreateRequest();
        request.Price = _request.MaximumBudget;

        Assert.True((await _service.CreateOfferAsync(request)).IsSuccess);
    }

    [Theory]
    [InlineData(OfferStatus.Pending)]
    [InlineData(OfferStatus.Rejected)]
    [InlineData(OfferStatus.Withdrawn)]
    public async Task Create_SecondOfferFromSameTradesperson_ReturnsConflict(OfferStatus status)
    {
        await AddOfferAsync(_profile, status);

        Assert.Equal(ErrorType.Conflict, (await _service.CreateOfferAsync(CreateRequest())).ErrorType);
        Assert.Single(await _ctx.Offers.ToListAsync());
    }

    [Theory]
    [InlineData(OfferStatus.Accepted)]
    [InlineData(OfferStatus.Rejected)]
    [InlineData(OfferStatus.Withdrawn)]
    [InlineData(OfferStatus.Closed)]
    public async Task Update_DecidedOfferCannotBeEditedOrDeleted(OfferStatus status)
    {
        var offer = await AddOfferAsync(_profile, status);
        var request = UpdateRequest(offer);
        request.Price += 1m;

        Assert.Equal(ErrorType.Conflict, (await _service.UpdateOfferAsync(request)).ErrorType);
        Assert.Equal(ErrorType.Conflict, (await _service.DeleteOfferAsync(new() { Id = offer.Id })).ErrorType);
        Assert.Equal(100m, offer.Price);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_CannotMoveOfferToAnotherRequestOrTradesperson(bool changeRequest)
    {
        var offer = await AddOfferAsync(_profile);
        var request = UpdateRequest(offer);
        if (changeRequest) request.ServiceRequestId = 999;
        else request.TradespersonProfileId = 999;

        Assert.Equal(ErrorType.Conflict, (await _service.UpdateOfferAsync(request)).ErrorType);
    }

    [Fact]
    public async Task Update_OverBudgetEdit_IsRejectedWithoutChangingOffer()
    {
        var offer = await AddOfferAsync(_profile);
        var request = UpdateRequest(offer);
        request.Price = _request.MaximumBudget + 1m;

        Assert.Equal(ErrorType.Conflict, (await _service.UpdateOfferAsync(request)).ErrorType);
        Assert.Equal(100m, offer.Price);
    }

    [Fact]
    public async Task Accept_ClosesPendingCompetitorsUpdatesRequestAndRecordsHistory()
    {
        var offer = await AddOfferAsync(_profile);
        var competing = await AddOfferAsync(await AddProfileAsync());
        var rejected = await AddOfferAsync(await AddProfileAsync(), OfferStatus.Rejected);
        var request = DecisionRequest(offer, OfferStatus.Accepted, _request.CustomerId);
        request.DecisionReason = "  Chosen offer  ";
        var beforeDecision = DateTime.UtcNow;

        Assert.True((await _service.UpdateOfferAsync(request)).IsSuccess);
        _ctx.ChangeTracker.Clear();
        var accepted = await _ctx.Offers.SingleAsync(item => item.Id == offer.Id);
        var closed = await _ctx.Offers.SingleAsync(item => item.Id == competing.Id);
        Assert.Equal(OfferStatus.Accepted, accepted.Status);
        Assert.Equal(_request.CustomerId, accepted.DecidedByUserId);
        Assert.Equal("Chosen offer", accepted.DecisionReason);
        Assert.InRange(accepted.DecidedAtUtc!.Value, beforeDecision, DateTime.UtcNow);
        Assert.Equal(OfferStatus.Closed, closed.Status);
        Assert.Equal(accepted.DecidedAtUtc, closed.DecidedAtUtc);
        Assert.Equal(_request.CustomerId, closed.DecidedByUserId);
        Assert.Equal(OfferStatus.Rejected, (await _ctx.Offers.SingleAsync(item => item.Id == rejected.Id)).Status);
        Assert.Equal(RequestStatus.OfferAccepted, (await _ctx.ServiceRequests.SingleAsync()).Status);
        var history = await _ctx.StatusHistories.SingleAsync();
        Assert.Equal(_request.Id, history.ServiceRequestId);
        Assert.Equal(_request.CustomerId, history.ChangedByUserId);
        Assert.Equal(RequestStatus.Created, history.PreviousStatus);
        Assert.Equal(RequestStatus.OfferAccepted, history.NewStatus);
        Assert.Equal(accepted.DecidedAtUtc, history.ChangedAtUtc);
        Assert.Empty(await _ctx.Appointments.ToListAsync());
        var response = (await _service.GetOfferByIdAsync(offer.Id)).Value!;
        Assert.Equal((int)OfferStatus.Accepted, response.Status);
        Assert.Equal(accepted.DecidedAtUtc, response.DecidedAtUtc);
        Assert.Equal("Chosen offer", response.DecisionReason);
        Assert.Equal(_request.CustomerId, response.DecidedByUserId);
        Assert.Equal(ErrorType.Conflict, (await _service.UpdateOfferAsync(request)).ErrorType);
        Assert.Single(await _ctx.StatusHistories.ToListAsync());
    }

    [Theory]
    [InlineData(OfferStatus.Rejected)]
    [InlineData(OfferStatus.Withdrawn)]
    public async Task RejectOrWithdraw_RecordsDecisionWithoutChangingRequest(OfferStatus status)
    {
        var offer = await AddOfferAsync(_profile);
        var actorId = status == OfferStatus.Withdrawn ? _profile.UserId : _request.CustomerId;

        Assert.True((await _service.UpdateOfferAsync(DecisionRequest(offer, status, actorId))).IsSuccess);
        Assert.Equal(status, offer.Status);
        Assert.Equal(actorId, offer.DecidedByUserId);
        Assert.NotNull(offer.DecidedAtUtc);
        Assert.Equal(RequestStatus.Created, _request.Status);
        Assert.Empty(await _ctx.StatusHistories.ToListAsync());
    }

    [Theory]
    [InlineData(OfferStatus.Accepted)]
    [InlineData(OfferStatus.Rejected)]
    [InlineData(OfferStatus.Withdrawn)]
    public async Task Decision_ByWrongParticipant_ReturnsForbidden(OfferStatus status)
    {
        var offer = await AddOfferAsync(_profile);
        var wrongActorId = status == OfferStatus.Withdrawn ? _request.CustomerId : _profile.UserId;

        Assert.Equal(ErrorType.Forbidden,
            (await _service.UpdateOfferAsync(DecisionRequest(offer, status, wrongActorId))).ErrorType);
        Assert.Equal(OfferStatus.Pending, offer.Status);
        Assert.Null(offer.DecidedAtUtc);
    }

    [Fact]
    public async Task Decision_MissingOrInactiveActor_IsRejected()
    {
        var offer = await AddOfferAsync(_profile);
        Assert.Equal(ErrorType.NotFound,
            (await _service.UpdateOfferAsync(DecisionRequest(offer, OfferStatus.Accepted, 999))).ErrorType);
        _request.Customer.IsActive = false;
        await _ctx.SaveChangesAsync();

        Assert.Equal(ErrorType.Forbidden,
            (await _service.UpdateOfferAsync(DecisionRequest(offer, OfferStatus.Accepted, _request.CustomerId))).ErrorType);
        Assert.Equal(OfferStatus.Pending, offer.Status);
    }

    [Fact]
    public async Task Decision_CannotChangeTermsAtTheSameTime()
    {
        var offer = await AddOfferAsync(_profile);
        var request = DecisionRequest(offer, OfferStatus.Accepted, _request.CustomerId);
        request.Price += 1m;

        Assert.Equal(ErrorType.Conflict, (await _service.UpdateOfferAsync(request)).ErrorType);
        Assert.Equal(100m, offer.Price);
        Assert.Equal(OfferStatus.Pending, offer.Status);
    }

    [Fact]
    public async Task Update_CannotManuallyCloseAnOffer()
    {
        var offer = await AddOfferAsync(_profile);

        Assert.Equal(ErrorType.Conflict,
            (await _service.UpdateOfferAsync(DecisionRequest(offer, OfferStatus.Closed, _request.CustomerId))).ErrorType);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Accept_OverlappingCustomerOrTradesperson_ReturnsConflictWithoutSideEffects(bool sameTradesperson)
    {
        var offer = await AddOfferAsync(_profile);
        var otherOffer = new Offer
        {
            TradespersonProfile = sameTradesperson ? _profile : await AddProfileAsync(),
            ServiceRequest = new ServiceRequest
            {
                Customer = sameTradesperson ? NewUser() : _request.Customer,
                City = _request.City, ServiceCategory = _request.ServiceCategory, MaximumBudget = 500m
            },
            Status = OfferStatus.Accepted
        };
        _ctx.Appointments.Add(new Appointment
        {
            Offer = otherOffer, StartUtc = _start.AddMinutes(30), EndUtc = _start.AddMinutes(90),
            Status = AppointmentStatus.Confirmed
        });
        await _ctx.SaveChangesAsync();

        var result = await _service.UpdateOfferAsync(DecisionRequest(offer, OfferStatus.Accepted, _request.CustomerId));

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(OfferStatus.Pending, offer.Status);
        Assert.Equal(RequestStatus.Created, _request.Status);
        Assert.Empty(await _ctx.StatusHistories.ToListAsync());
    }

    [Fact]
    public async Task Create_RequestWithExistingAcceptedOffer_ReturnsConflict()
    {
        await AddOfferAsync(await AddProfileAsync(), OfferStatus.Accepted);

        Assert.Equal(ErrorType.Conflict, (await _service.CreateOfferAsync(CreateRequest())).ErrorType);
    }

    [Fact]
    public async Task Delete_OfferWithAppointment_ReturnsConflict()
    {
        var offer = await AddOfferAsync(_profile);
        _ctx.Appointments.Add(new Appointment { OfferId = offer.Id, StartUtc = _start, EndUtc = _start.AddHours(1) });
        await _ctx.SaveChangesAsync();

        Assert.Equal(ErrorType.Conflict, (await _service.DeleteOfferAsync(new() { Id = offer.Id })).ErrorType);
        Assert.Single(await _ctx.Offers.ToListAsync());
    }

    [Fact]
    public async Task MissingOffer_ReturnsNotFoundForReadUpdateAndDelete()
    {
        Assert.Equal(ErrorType.NotFound, (await _service.GetOfferByIdAsync(999)).ErrorType);
        Assert.Equal(ErrorType.NotFound, (await _service.UpdateOfferAsync(new() { Id = 999 })).ErrorType);
        Assert.Equal(ErrorType.NotFound, (await _service.DeleteOfferAsync(new() { Id = 999 })).ErrorType);
    }

    private OfferCreateRequest CreateRequest() => new()
    {
        ServiceRequestId = _request.Id, TradespersonProfileId = _profile.Id,
        Price = 100m, ProposedStartUtc = _start, DurationMinutes = 60
    };

    private static OfferUpdateRequest UpdateRequest(Offer offer) => new()
    {
        Id = offer.Id, ServiceRequestId = offer.ServiceRequestId, TradespersonProfileId = offer.TradespersonProfileId,
        Price = offer.Price, ProposedStartUtc = offer.ProposedStartUtc, DurationMinutes = offer.DurationMinutes,
        Status = (int)offer.Status
    };

    private static OfferUpdateRequest DecisionRequest(Offer offer, OfferStatus status, int actorId)
    {
        var request = UpdateRequest(offer);
        request.Status = (int)status;
        request.DecidedByUserId = actorId;
        return request;
    }

    private async Task<Offer> AddOfferAsync(TradespersonProfile profile, OfferStatus status = OfferStatus.Pending)
    {
        var offer = new Offer
        {
            ServiceRequest = _request, TradespersonProfile = profile,
            Price = 100m, ProposedStartUtc = _start, DurationMinutes = 60, Status = status
        };
        _ctx.Offers.Add(offer);
        await _ctx.SaveChangesAsync();
        return offer;
    }

    private async Task<TradespersonProfile> AddProfileAsync()
    {
        var profile = new TradespersonProfile { User = NewUser(), City = _request.City, IsApproved = true };
        _ctx.TradespersonServices.Add(new TradespersonService
        {
            TradespersonProfile = profile, ServiceCategory = _request.ServiceCategory, Name = "Repairs"
        });
        await _ctx.SaveChangesAsync();
        return profile;
    }

    private static ApplicationUser NewUser() => new()
    {
        UserName = Guid.NewGuid().ToString(), PasswordHash = "test-hash", PasswordSalt = "test-salt"
    };

    public void Dispose() => _ctx.Dispose();
}
