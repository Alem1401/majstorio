using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

public class AppointmentServiceTests : IDisposable
{
    private readonly MajstorioDbContext _ctx = TestSupport.NewInMemoryContext();
    private readonly Scenario _s;
    private readonly AppointmentService _service;
    private readonly ServiceRequestService _requests;

    public AppointmentServiceTests()
    {
        _s = new Scenario(_ctx);
        _service = new AppointmentService(_ctx, TestSupport.NewMapper());
        _requests = new ServiceRequestService(_ctx, TestSupport.NewMapper());
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task Complete_ByTheAssignedTradesperson_CompletesRequestAndAppointmentWithHistory()
    {
        var appointment = await AddStartedAppointmentAsync();

        var result = await _service.CompleteAppointmentAsync(TestSupport.As(_s.Profile.User), appointment.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
        Assert.Equal(RequestStatus.Completed, _s.Request.Status);
        var history = await _ctx.StatusHistories.OrderBy(row => row.Id).LastAsync();
        Assert.Equal(_s.Profile.UserId, history.ChangedByUserId);
        Assert.Equal(RequestStatus.AppointmentConfirmed, history.PreviousStatus);
        Assert.Equal(RequestStatus.Completed, history.NewStatus);
        Assert.Equal(AppointmentStatus.Confirmed, history.PreviousAppointmentStatus);
        Assert.Equal(AppointmentStatus.Completed, history.NewAppointmentStatus);
    }

    [Fact]
    public async Task Complete_ByTheCustomerOrAnotherTradesperson_IsForbidden()
    {
        var appointment = await AddStartedAppointmentAsync();

        Assert.Equal(ErrorType.Forbidden,
            (await _service.CompleteAppointmentAsync(TestSupport.As(_s.Customer), appointment.Id)).ErrorType);
        Assert.Equal(ErrorType.Forbidden,
            (await _service.CompleteAppointmentAsync(TestSupport.As(_s.AddProfile().User), appointment.Id)).ErrorType);
        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
    }

    [Fact]
    public async Task Complete_BeforeTheAppointmentStarts_ReturnsConflict()
    {
        var appointment = await AddStartedAppointmentAsync(started: false);

        var result = await _service.CompleteAppointmentAsync(TestSupport.As(_s.Profile.User), appointment.Id);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(RequestStatus.AppointmentConfirmed, _s.Request.Status);
    }

    [Fact]
    public async Task Complete_Twice_ReturnsConflictAndWritesNoSecondHistoryRow()
    {
        var appointment = await AddStartedAppointmentAsync();
        var tradesperson = TestSupport.As(_s.Profile.User);
        Assert.True((await _service.CompleteAppointmentAsync(tradesperson, appointment.Id)).IsSuccess);
        var historyCount = await _ctx.StatusHistories.CountAsync();

        var again = await _service.CompleteAppointmentAsync(tradesperson, appointment.Id);

        Assert.Equal(ErrorType.Conflict, again.ErrorType);
        Assert.Equal(historyCount, await _ctx.StatusHistories.CountAsync());
    }

    [Fact]
    public async Task Complete_CancelledAppointment_ReturnsConflict()
    {
        var appointment = await AddStartedAppointmentAsync();
        Assert.True((await _requests.CancelServiceRequestAsync(TestSupport.As(_s.Customer), new() { Id = _s.Request.Id })).IsSuccess);

        var result = await _service.CompleteAppointmentAsync(TestSupport.As(_s.Profile.User), appointment.Id);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
    }

    [Fact]
    public async Task Cancel_AfterCompletion_ReturnsConflict()
    {
        var appointment = await AddStartedAppointmentAsync();
        Assert.True((await _service.CompleteAppointmentAsync(TestSupport.As(_s.Profile.User), appointment.Id)).IsSuccess);

        var result = await _requests.CancelServiceRequestAsync(TestSupport.As(_s.Customer), new() { Id = _s.Request.Id });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(RequestStatus.Completed, _s.Request.Status);
        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyAppointmentsTheUserTakesPartIn()
    {
        var appointment = await AddStartedAppointmentAsync();
        var bystander = _s.AddProfile();

        var forCustomer = (await _service.GetMyAppointmentsAsync(TestSupport.As(_s.Customer), new())).Value!;
        var forTradesperson = (await _service.GetMyAppointmentsAsync(TestSupport.As(_s.Profile.User), new())).Value!;
        var forBystander = (await _service.GetMyAppointmentsAsync(TestSupport.As(bystander.User), new())).Value!;

        Assert.Equal(appointment.Id, Assert.Single(forCustomer.Items).Id);
        Assert.Equal(appointment.Id, Assert.Single(forTradesperson.Items).Id);
        Assert.Empty(forBystander.Items);
    }

    [Fact]
    public async Task MissingAppointment_ReturnsNotFound()
    {
        Assert.Equal(ErrorType.NotFound, (await _service.GetAppointmentByIdAsync(999)).ErrorType);
        Assert.Equal(ErrorType.NotFound,
            (await _service.CompleteAppointmentAsync(TestSupport.As(_s.Profile.User), 999)).ErrorType);
    }

    // Accepts an offer through the real flow, then moves the appointment into the past so it can be completed.
    private async Task<Appointment> AddStartedAppointmentAsync(bool started = true)
    {
        var offer = _s.AddOffer();
        var offers = new OfferService(_ctx, TestSupport.NewMapper());
        Assert.True((await offers.AcceptOfferAsync(TestSupport.As(_s.Customer), offer.Id)).IsSuccess);
        var appointment = await _ctx.Appointments.SingleAsync();
        if (started)
        {
            appointment.StartUtc = DateTime.UtcNow.AddHours(-2);
            appointment.EndUtc = DateTime.UtcNow.AddHours(-1);
            await _ctx.SaveChangesAsync();
        }

        return appointment;
    }
}
