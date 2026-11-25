using Majstorio.Model.Requests.ServiceRequest;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

public class ServiceRequestServiceTests : IDisposable
{
    private readonly MajstorioDbContext _ctx = TestSupport.NewInMemoryContext();
    private readonly Scenario _s;
    private readonly ServiceRequestService _service;
    private readonly OfferService _offers;

    public ServiceRequestServiceTests()
    {
        _s = new Scenario(_ctx);
        _service = new ServiceRequestService(_ctx, TestSupport.NewMapper(), new FakeMessagePublisher());
        _offers = new OfferService(_ctx, TestSupport.NewMapper(), new FakeMessagePublisher());
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task Create_SetsCustomerFromTheTokenAndStartsAsCreatedWithHistory()
    {
        var customer = TestSupport.NewUser();
        _ctx.Add(customer);
        await _ctx.SaveChangesAsync();

        var result = await _service.CreateServiceRequestAsync(TestSupport.As(customer), CreateRequest());

        Assert.True(result.IsSuccess);
        var created = await _ctx.ServiceRequests.SingleAsync(request => request.CustomerId == customer.Id);
        Assert.Equal(RequestStatus.Created, created.Status);
        Assert.Equal(_s.City.Id, created.CityId);
        Assert.Equal(_s.Category.Id, created.ServiceCategoryId);
        Assert.Equal(250m, created.MaximumBudget);
        var history = await _ctx.StatusHistories.SingleAsync(history => history.ServiceRequestId == created.Id);
        Assert.Null(history.PreviousStatus);
        Assert.Equal(RequestStatus.Created, history.NewStatus);
        Assert.Equal(customer.Id, history.ChangedByUserId);
    }

    [Fact]
    public void CreateRequest_HasNoCustomerOrStatusField()
    {
        // Nothing the client sends can choose the owner or the initial status.
        var names = typeof(ServiceRequestCreateRequest).GetProperties().Select(property => property.Name);

        Assert.DoesNotContain("CustomerId", names);
        Assert.DoesNotContain("Status", names);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_InactiveCategoryOrCity_ReturnsNotFound(bool inactiveCity)
    {
        if (inactiveCity) _s.City.IsActive = false;
        else _s.Category.IsActive = false;
        await _ctx.SaveChangesAsync();

        var result = await _service.CreateServiceRequestAsync(TestSupport.As(_s.Customer), CreateRequest());

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task List_CustomerSeesOnlyOwnRequests_AdministratorSeesAll()
    {
        var other = TestSupport.NewUser();
        _s.AddRequest(other);

        var own = (await _service.GetServiceRequestsAsync(TestSupport.As(_s.Customer), new())).Value!;
        var all = (await _service.GetServiceRequestsAsync(TestSupport.As(_s.Administrator), new())).Value!;

        Assert.Equal(_s.Request.Id, Assert.Single(own.Items).Id);
        Assert.Equal(2, all.TotalCount);
    }

    [Fact]
    public async Task List_TradespersonSeesOnlyOpenRequestsOfTheirCategoryAndCity()
    {
        var otherCity = new City { Name = "Other city" };
        var otherCategory = new ServiceCategory { Name = "Other category" };
        _s.AddRequest(city: otherCity);
        _s.AddRequest(category: otherCategory);
        var targetedElsewhere = _s.AddRequest();
        targetedElsewhere.TargetTradespersonProfileId = _s.AddProfile().Id;
        var closed = _s.AddRequest();
        closed.Status = RequestStatus.Cancelled;
        await _ctx.SaveChangesAsync();

        var visible = (await _service.GetServiceRequestsAsync(TestSupport.As(_s.Profile.User), new())).Value!;

        Assert.Equal(_s.Request.Id, Assert.Single(visible.Items).Id);
    }

    [Fact]
    public async Task List_UnapprovedOrInactiveTradesperson_SeesNoOpenRequests()
    {
        _s.Profile.IsActive = false;
        await _ctx.SaveChangesAsync();

        var visible = (await _service.GetServiceRequestsAsync(TestSupport.As(_s.Profile.User), new())).Value!;

        Assert.Empty(visible.Items);
    }

    [Fact]
    public async Task List_TradespersonKeepsSeeingARequestTheyMadeAnOfferOn()
    {
        var offer = _s.AddOffer();
        Assert.True((await _offers.AcceptOfferAsync(TestSupport.As(_s.Customer), offer.Id)).IsSuccess);
        var bystander = _s.AddProfile();

        var assigned = (await _service.GetServiceRequestsAsync(TestSupport.As(_s.Profile.User), new())).Value!;
        var others = (await _service.GetServiceRequestsAsync(TestSupport.As(bystander.User), new())).Value!;

        Assert.Single(assigned.Items);
        Assert.Empty(others.Items);
    }

    [Fact]
    public async Task List_FiltersByStatusAndPages()
    {
        var cancelled = _s.AddRequest();
        cancelled.Status = RequestStatus.Cancelled;
        _s.AddRequest();
        await _ctx.SaveChangesAsync();
        var customer = TestSupport.As(_s.Customer);

        var byStatus = (await _service.GetServiceRequestsAsync(customer, new() { Status = (int)RequestStatus.Cancelled })).Value!;
        var firstPage = (await _service.GetServiceRequestsAsync(customer, new() { PageSize = 2 })).Value!;

        Assert.Equal(cancelled.Id, Assert.Single(byStatus.Items).Id);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(3, firstPage.TotalCount);
    }

    [Fact]
    public async Task Detail_OfAnotherCustomersRequest_IsNotFound()
    {
        var stranger = TestSupport.NewUser();
        _ctx.Add(stranger);
        await _ctx.SaveChangesAsync();

        var result = await _service.GetServiceRequestByIdAsync(TestSupport.As(stranger), _s.Request.Id);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Detail_CustomerSeesAllOffers_TradespersonOnlyTheirOwn_WithAppointmentAndHistory()
    {
        var mine = _s.AddOffer();
        var competitor = _s.AddProfile();
        _s.AddOffer(competitor);
        Assert.True((await _offers.AcceptOfferAsync(TestSupport.As(_s.Customer), mine.Id)).IsSuccess);

        var forCustomer = (await _service.GetServiceRequestByIdAsync(TestSupport.As(_s.Customer), _s.Request.Id)).Value!;
        var forCompetitor = (await _service.GetServiceRequestByIdAsync(TestSupport.As(competitor.User), _s.Request.Id)).Value!;

        Assert.Equal(2, forCustomer.Offers.Count);
        Assert.NotNull(forCustomer.Appointment);
        Assert.Equal((int)AppointmentStatus.Confirmed, forCustomer.Appointment!.Status);
        Assert.Equal(
            [(int)RequestStatus.OfferAccepted, (int)RequestStatus.AppointmentConfirmed],
            forCustomer.StatusHistory.Select(history => history.NewStatus));
        Assert.Equal(competitor.Id, Assert.Single(forCompetitor.Offers).TradespersonProfileId);
        Assert.Null(forCompetitor.Appointment);
    }

    [Fact]
    public async Task UpdateAndDelete_OnlyByTheOwnerAndOnlyWhileThereAreNoOffers()
    {
        var stranger = TestSupport.NewUser();
        _ctx.Add(stranger);
        await _ctx.SaveChangesAsync();
        var update = new ServiceRequestUpdateRequest
        {
            Id = _s.Request.Id, CityId = _s.City.Id, ServiceCategoryId = _s.Category.Id,
            Description = "Changed", DesiredStartUtc = _s.Start, MaximumBudget = 300m
        };
        var delete = new ServiceRequestDeleteRequest { Id = _s.Request.Id };

        Assert.Equal(ErrorType.Forbidden, (await _service.UpdateServiceRequestAsync(TestSupport.As(stranger), update)).ErrorType);
        Assert.Equal(ErrorType.Forbidden, (await _service.DeleteServiceRequestAsync(TestSupport.As(stranger), delete)).ErrorType);

        Assert.True((await _service.UpdateServiceRequestAsync(TestSupport.As(_s.Customer), update)).IsSuccess);
        Assert.Equal("Changed", _s.Request.Description);
        Assert.Equal(_s.Customer.Id, _s.Request.CustomerId);

        _s.AddOffer();
        Assert.Equal(ErrorType.Conflict, (await _service.UpdateServiceRequestAsync(TestSupport.As(_s.Customer), update)).ErrorType);
        Assert.Equal(ErrorType.Conflict, (await _service.DeleteServiceRequestAsync(TestSupport.As(_s.Customer), delete)).ErrorType);
    }

    [Fact]
    public async Task Delete_OpenRequestWithoutOffers_RemovesIt()
    {
        var result = await _service.DeleteServiceRequestAsync(TestSupport.As(_s.Customer), new() { Id = _s.Request.Id });

        Assert.True(result.IsSuccess);
        Assert.Empty(await _ctx.ServiceRequests.ToListAsync());
    }

    [Fact]
    public async Task Cancel_OpenRequest_ClosesPendingOffersAndWritesHistory()
    {
        var offer = _s.AddOffer();

        var result = await _service.CancelServiceRequestAsync(TestSupport.As(_s.Customer),
            new() { Id = _s.Request.Id, Reason = " Solved it myself " });

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestStatus.Cancelled, _s.Request.Status);
        Assert.Equal(OfferStatus.Closed, offer.Status);
        var history = await _ctx.StatusHistories.SingleAsync();
        Assert.Equal(RequestStatus.Created, history.PreviousStatus);
        Assert.Equal(RequestStatus.Cancelled, history.NewStatus);
        Assert.Equal(_s.Customer.Id, history.ChangedByUserId);
        Assert.Equal("Solved it myself", history.Note);
    }

    [Theory]
    [InlineData("customer")]
    [InlineData("tradesperson")]
    [InlineData("administrator")]
    public async Task Cancel_AfterAcceptance_AlsoCancelsTheAppointment(string actor)
    {
        var offer = _s.AddOffer();
        Assert.True((await _offers.AcceptOfferAsync(TestSupport.As(_s.Customer), offer.Id)).IsSuccess);
        var user = actor switch
        {
            "customer" => _s.Customer,
            "tradesperson" => _s.Profile.User,
            _ => _s.Administrator
        };

        var result = await _service.CancelServiceRequestAsync(TestSupport.As(user), new() { Id = _s.Request.Id });

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestStatus.Cancelled, _s.Request.Status);
        Assert.Equal(AppointmentStatus.Cancelled, (await _ctx.Appointments.SingleAsync()).Status);
        var history = await _ctx.StatusHistories.OrderBy(history => history.Id).LastAsync();
        Assert.Equal(user.Id, history.ChangedByUserId);
        Assert.Equal(AppointmentStatus.Confirmed, history.PreviousAppointmentStatus);
        Assert.Equal(AppointmentStatus.Cancelled, history.NewAppointmentStatus);
    }

    [Fact]
    public async Task Cancel_ByAnUnrelatedUser_IsForbidden()
    {
        var bystander = _s.AddProfile();

        var result = await _service.CancelServiceRequestAsync(TestSupport.As(bystander.User), new() { Id = _s.Request.Id });

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal(RequestStatus.Created, _s.Request.Status);
    }

    [Theory]
    [InlineData(RequestStatus.Completed)]
    [InlineData(RequestStatus.Cancelled)]
    public async Task Cancel_CompletedOrAlreadyCancelledRequest_ReturnsConflict(RequestStatus status)
    {
        _s.Request.Status = status;
        await _ctx.SaveChangesAsync();

        var result = await _service.CancelServiceRequestAsync(TestSupport.As(_s.Customer), new() { Id = _s.Request.Id });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(status, _s.Request.Status);
        Assert.Empty(await _ctx.StatusHistories.ToListAsync());
    }

    private ServiceRequestCreateRequest CreateRequest() => new()
    {
        CityId = _s.City.Id, ServiceCategoryId = _s.Category.Id, Description = "Broken socket",
        DesiredStartUtc = _s.Start, MaximumBudget = 250m
    };
}
