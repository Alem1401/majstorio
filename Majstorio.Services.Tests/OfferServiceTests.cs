using Majstorio.Model.Requests.Offer;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

public class OfferServiceTests : IDisposable
{
    private readonly MajstorioDbContext _ctx = TestSupport.NewInMemoryContext();
    private readonly Scenario _s;
    private readonly OfferService _service;

    public OfferServiceTests()
    {
        _s = new Scenario(_ctx);
        _service = new OfferService(_ctx, TestSupport.NewMapper(), new FakeMessagePublisher());
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task Create_UsesTheProfileOfTheSignedInTradesperson()
    {
        var result = await _service.CreateOfferAsync(TestSupport.As(_s.Profile.User), CreateRequest());

        Assert.True(result.IsSuccess);
        var offer = await _ctx.Offers.SingleAsync();
        Assert.Equal(_s.Profile.Id, offer.TradespersonProfileId);
        Assert.Equal(OfferStatus.Pending, offer.Status);
        Assert.Equal(100m, offer.Price);
        Assert.DoesNotContain("TradespersonProfileId",
            typeof(OfferCreateRequest).GetProperties().Select(property => property.Name));
    }

    [Fact]
    public async Task Create_ByAUserWithoutAProfile_IsForbidden()
    {
        var result = await _service.CreateOfferAsync(TestSupport.As(_s.Customer), CreateRequest());

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Empty(await _ctx.Offers.ToListAsync());
    }

    [Theory]
    [InlineData(OfferStatus.Pending)]
    [InlineData(OfferStatus.Withdrawn)]
    public async Task Create_SecondOfferFromTheSameTradesperson_ReturnsConflict(OfferStatus existing)
    {
        _s.AddOffer(status: existing);

        var result = await _service.CreateOfferAsync(TestSupport.As(_s.Profile.User), CreateRequest());

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Single(await _ctx.Offers.ToListAsync());
    }

    [Theory]
    [InlineData("inactive profile")]
    [InlineData("unapproved profile")]
    [InlineData("other city")]
    [InlineData("other category")]
    public async Task Create_TradespersonWhoDoesNotMatchTheRequest_IsForbidden(string reason)
    {
        if (reason == "inactive profile") _s.Profile.IsActive = false;
        if (reason == "unapproved profile") _s.Profile.IsApproved = false;
        if (reason == "other city") _s.Profile.City = new City { Name = "Elsewhere" };
        if (reason == "other category") (await _ctx.TradespersonServices.SingleAsync()).IsActive = false;
        await _ctx.SaveChangesAsync();

        var result = await _service.CreateOfferAsync(TestSupport.As(_s.Profile.User), CreateRequest());

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Create_PriceAboveBudgetOrClosedRequest_ReturnsConflict()
    {
        var tradesperson = TestSupport.As(_s.Profile.User);
        var tooExpensive = CreateRequest();
        tooExpensive.Price = _s.Request.MaximumBudget + 0.01m;
        Assert.Equal(ErrorType.Conflict, (await _service.CreateOfferAsync(tradesperson, tooExpensive)).ErrorType);

        _s.Request.Status = RequestStatus.Cancelled;
        await _ctx.SaveChangesAsync();
        Assert.Equal(ErrorType.Conflict, (await _service.CreateOfferAsync(tradesperson, CreateRequest())).ErrorType);
    }

    [Fact]
    public async Task Update_OnlyTheOwnerCanChangeAPendingOffer()
    {
        var offer = _s.AddOffer();
        var update = new OfferUpdateRequest
        {
            Id = offer.Id, Price = 150m, ProposedStartUtc = _s.Start.AddDays(1), DurationMinutes = 90
        };
        var other = _s.AddProfile();

        Assert.Equal(ErrorType.Forbidden, (await _service.UpdateOfferAsync(TestSupport.As(other.User), update)).ErrorType);
        Assert.True((await _service.UpdateOfferAsync(TestSupport.As(_s.Profile.User), update)).IsSuccess);
        Assert.Equal(150m, offer.Price);
        Assert.Equal(OfferStatus.Pending, offer.Status);

        offer.Status = OfferStatus.Accepted;
        await _ctx.SaveChangesAsync();
        Assert.Equal(ErrorType.Conflict, (await _service.UpdateOfferAsync(TestSupport.As(_s.Profile.User), update)).ErrorType);
    }

    [Fact]
    public async Task Withdraw_PendingOffer_ByItsTradesperson_Succeeds()
    {
        var offer = _s.AddOffer();

        var result = await _service.WithdrawOfferAsync(TestSupport.As(_s.Profile.User), offer.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(OfferStatus.Withdrawn, offer.Status);
        Assert.Equal(_s.Profile.UserId, offer.DecidedByUserId);
        Assert.NotNull(offer.DecidedAtUtc);
        Assert.Equal(RequestStatus.Created, _s.Request.Status);
    }

    [Fact]
    public async Task Withdraw_AcceptedOffer_ReturnsConflict()
    {
        var offer = _s.AddOffer();
        Assert.True((await _service.AcceptOfferAsync(TestSupport.As(_s.Customer), offer.Id)).IsSuccess);

        var result = await _service.WithdrawOfferAsync(TestSupport.As(_s.Profile.User), offer.Id);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(OfferStatus.Accepted, offer.Status);
    }

    [Fact]
    public async Task Withdraw_ByAnotherUser_IsForbidden()
    {
        var offer = _s.AddOffer();

        Assert.Equal(ErrorType.Forbidden, (await _service.WithdrawOfferAsync(TestSupport.As(_s.Customer), offer.Id)).ErrorType);
        Assert.Equal(ErrorType.Forbidden, (await _service.WithdrawOfferAsync(TestSupport.As(_s.AddProfile().User), offer.Id)).ErrorType);
        Assert.Equal(OfferStatus.Pending, offer.Status);
    }

    [Fact]
    public async Task Accept_ClosesOtherOffersCreatesConfirmedAppointmentMovesRequestAndWritesHistory()
    {
        var offer = _s.AddOffer();
        var competing = _s.AddOffer(_s.AddProfile());
        var withdrawn = _s.AddOffer(_s.AddProfile(), status: OfferStatus.Withdrawn);
        var before = DateTime.UtcNow;

        var result = await _service.AcceptOfferAsync(TestSupport.As(_s.Customer), offer.Id);

        Assert.True(result.IsSuccess);
        _ctx.ChangeTracker.Clear();
        var accepted = await _ctx.Offers.SingleAsync(item => item.Id == offer.Id);
        Assert.Equal(OfferStatus.Accepted, accepted.Status);
        Assert.Equal(_s.Customer.Id, accepted.DecidedByUserId);
        Assert.InRange(accepted.DecidedAtUtc!.Value, before, DateTime.UtcNow);
        Assert.Equal(OfferStatus.Closed, (await _ctx.Offers.SingleAsync(item => item.Id == competing.Id)).Status);
        Assert.Equal(OfferStatus.Withdrawn, (await _ctx.Offers.SingleAsync(item => item.Id == withdrawn.Id)).Status);

        var appointment = await _ctx.Appointments.SingleAsync();
        Assert.Equal(offer.Id, appointment.OfferId);
        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
        Assert.Equal(_s.Start, appointment.StartUtc);
        Assert.Equal(_s.Start.AddMinutes(60), appointment.EndUtc);
        Assert.Equal(RequestStatus.AppointmentConfirmed, (await _ctx.ServiceRequests.SingleAsync()).Status);

        var history = await _ctx.StatusHistories.OrderBy(row => row.Id).ToListAsync();
        Assert.Equal(2, history.Count);
        Assert.All(history, row =>
        {
            Assert.Equal(_s.Customer.Id, row.ChangedByUserId);
            Assert.Equal(_s.Request.Id, row.ServiceRequestId);
            Assert.Equal(appointment.Id, row.AppointmentId);
            Assert.Equal(accepted.DecidedAtUtc, row.ChangedAtUtc);
        });
        Assert.Equal((RequestStatus.Created, RequestStatus.OfferAccepted), (history[0].PreviousStatus!.Value, history[0].NewStatus));
        Assert.Null(history[0].PreviousAppointmentStatus);
        Assert.Equal(AppointmentStatus.Pending, history[0].NewAppointmentStatus);
        Assert.Equal((RequestStatus.OfferAccepted, RequestStatus.AppointmentConfirmed), (history[1].PreviousStatus!.Value, history[1].NewStatus));
        Assert.Equal(AppointmentStatus.Pending, history[1].PreviousAppointmentStatus);
        Assert.Equal(AppointmentStatus.Confirmed, history[1].NewAppointmentStatus);
    }

    [Fact]
    public async Task Accept_ByAnyoneButTheRequestOwner_IsForbidden()
    {
        var offer = _s.AddOffer();
        var stranger = TestSupport.NewUser();
        _ctx.Add(stranger);
        await _ctx.SaveChangesAsync();

        Assert.Equal(ErrorType.Forbidden, (await _service.AcceptOfferAsync(TestSupport.As(stranger), offer.Id)).ErrorType);
        Assert.Equal(ErrorType.Forbidden, (await _service.AcceptOfferAsync(TestSupport.As(_s.Profile.User), offer.Id)).ErrorType);
        Assert.Equal(ErrorType.Forbidden, (await _service.AcceptOfferAsync(TestSupport.As(_s.Administrator), offer.Id)).ErrorType);
        Assert.Equal(OfferStatus.Pending, offer.Status);
        Assert.Empty(await _ctx.Appointments.ToListAsync());
    }

    [Fact]
    public async Task Accept_Repeated_ReturnsConflictWithoutASecondAppointmentOrHistory()
    {
        var offer = _s.AddOffer();
        var other = _s.AddOffer(_s.AddProfile());
        var customer = TestSupport.As(_s.Customer);
        Assert.True((await _service.AcceptOfferAsync(customer, offer.Id)).IsSuccess);

        var sameAgain = await _service.AcceptOfferAsync(customer, offer.Id);
        var otherOffer = await _service.AcceptOfferAsync(customer, other.Id);

        Assert.Equal(ErrorType.Conflict, sameAgain.ErrorType);
        Assert.Equal(ErrorType.Conflict, otherOffer.ErrorType);
        Assert.Single(await _ctx.Appointments.ToListAsync());
        Assert.Equal(2, await _ctx.StatusHistories.CountAsync());
        Assert.Equal(OfferStatus.Closed, other.Status);
    }

    [Fact]
    public async Task Accept_OverlappingAppointmentOfTheSameTradesperson_ReturnsConflictWithoutSideEffects()
    {
        var firstCustomer = TestSupport.NewUser();
        var first = _s.AddOffer(request: _s.AddRequest(firstCustomer), start: _s.Start.AddMinutes(30));
        Assert.True((await _service.AcceptOfferAsync(TestSupport.As(firstCustomer), first.Id)).IsSuccess);
        var overlapping = _s.AddOffer();

        var result = await _service.AcceptOfferAsync(TestSupport.As(_s.Customer), overlapping.Id);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(OfferStatus.Pending, overlapping.Status);
        Assert.Equal(RequestStatus.Created, _s.Request.Status);
        Assert.Single(await _ctx.Appointments.ToListAsync());
        Assert.Equal(2, await _ctx.StatusHistories.CountAsync());
    }

    [Fact]
    public async Task Accept_AppointmentRightAfterAnotherOne_IsAllowed()
    {
        var firstCustomer = TestSupport.NewUser();
        var first = _s.AddOffer(request: _s.AddRequest(firstCustomer), start: _s.Start.AddMinutes(60));
        Assert.True((await _service.AcceptOfferAsync(TestSupport.As(firstCustomer), first.Id)).IsSuccess);
        var adjacent = _s.AddOffer();

        Assert.True((await _service.AcceptOfferAsync(TestSupport.As(_s.Customer), adjacent.Id)).IsSuccess);
        Assert.Equal(2, await _ctx.Appointments.CountAsync());
    }

    [Fact]
    public async Task Accept_OverlapWithACancelledAppointment_IsAllowed()
    {
        var firstCustomer = TestSupport.NewUser();
        var firstRequest = _s.AddRequest(firstCustomer);
        var first = _s.AddOffer(request: firstRequest);
        Assert.True((await _service.AcceptOfferAsync(TestSupport.As(firstCustomer), first.Id)).IsSuccess);
        var requests = new ServiceRequestService(_ctx, TestSupport.NewMapper());
        Assert.True((await requests.CancelServiceRequestAsync(TestSupport.As(firstCustomer), new() { Id = firstRequest.Id })).IsSuccess);

        Assert.True((await _service.AcceptOfferAsync(TestSupport.As(_s.Customer), _s.AddOffer().Id)).IsSuccess);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyOffersTheUserTakesPartIn()
    {
        var mine = _s.AddOffer();
        var competitor = _s.AddProfile();
        _s.AddOffer(competitor);
        _s.AddOffer(competitor, _s.AddRequest(TestSupport.NewUser()));

        var forTradesperson = (await _service.GetMyOffersAsync(TestSupport.As(_s.Profile.User), new())).Value!;
        var forCustomer = (await _service.GetMyOffersAsync(TestSupport.As(_s.Customer), new())).Value!;
        var filtered = (await _service.GetMyOffersAsync(TestSupport.As(competitor.User),
            new() { ServiceRequestId = _s.Request.Id })).Value!;

        Assert.Equal(mine.Id, Assert.Single(forTradesperson.Items).Id);
        Assert.Equal(2, forCustomer.TotalCount);
        Assert.Single(filtered.Items);
    }

    [Fact]
    public async Task MissingOffer_ReturnsNotFound()
    {
        var user = TestSupport.As(_s.Customer);

        Assert.Equal(ErrorType.NotFound, (await _service.GetOfferByIdAsync(999)).ErrorType);
        Assert.Equal(ErrorType.NotFound, (await _service.AcceptOfferAsync(user, 999)).ErrorType);
        Assert.Equal(ErrorType.NotFound, (await _service.WithdrawOfferAsync(user, 999)).ErrorType);
        Assert.Equal(ErrorType.NotFound, (await _service.UpdateOfferAsync(user, new() { Id = 999 })).ErrorType);
        Assert.Equal(ErrorType.NotFound, (await _service.DeleteOfferAsync(new() { Id = 999 })).ErrorType);
    }

    private OfferCreateRequest CreateRequest() => new()
    {
        ServiceRequestId = _s.Request.Id, Price = 100m, ProposedStartUtc = _s.Start, DurationMinutes = 60
    };
}
