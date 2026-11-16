using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.RequestStateMachine;
using Xunit;

namespace Majstorio.Services.Tests;

public class StatusTransitionsTests
{
    [Theory]
    [InlineData(RequestStatus.Created, RequestStatus.OfferAccepted, true)]
    [InlineData(RequestStatus.Created, RequestStatus.Cancelled, true)]
    [InlineData(RequestStatus.OfferAccepted, RequestStatus.AppointmentConfirmed, true)]
    [InlineData(RequestStatus.OfferAccepted, RequestStatus.Cancelled, true)]
    [InlineData(RequestStatus.AppointmentConfirmed, RequestStatus.Completed, true)]
    [InlineData(RequestStatus.AppointmentConfirmed, RequestStatus.Cancelled, true)]
    [InlineData(RequestStatus.Created, RequestStatus.AppointmentConfirmed, false)]
    [InlineData(RequestStatus.Created, RequestStatus.Completed, false)]
    [InlineData(RequestStatus.OfferAccepted, RequestStatus.Completed, false)]
    [InlineData(RequestStatus.Completed, RequestStatus.Cancelled, false)]
    [InlineData(RequestStatus.Completed, RequestStatus.Created, false)]
    [InlineData(RequestStatus.Cancelled, RequestStatus.Created, false)]
    [InlineData(RequestStatus.Cancelled, RequestStatus.Completed, false)]
    public void RequestMoves_FollowTheFlowOfThePlan(RequestStatus from, RequestStatus to, bool allowed)
    {
        Assert.Equal(allowed, StatusTransitions.IsAllowed(from, to));
    }

    [Theory]
    [InlineData(null, AppointmentStatus.Pending, true)]
    [InlineData(null, AppointmentStatus.Confirmed, false)]
    [InlineData(AppointmentStatus.Pending, AppointmentStatus.Confirmed, true)]
    [InlineData(AppointmentStatus.Pending, AppointmentStatus.Completed, false)]
    [InlineData(AppointmentStatus.Confirmed, AppointmentStatus.Completed, true)]
    [InlineData(AppointmentStatus.Confirmed, AppointmentStatus.Cancelled, true)]
    [InlineData(AppointmentStatus.Confirmed, AppointmentStatus.Pending, false)]
    [InlineData(AppointmentStatus.Completed, AppointmentStatus.Cancelled, false)]
    [InlineData(AppointmentStatus.Cancelled, AppointmentStatus.Confirmed, false)]
    public void AppointmentMoves_FollowTheFlowOfThePlan(AppointmentStatus? from, AppointmentStatus to, bool allowed)
    {
        Assert.Equal(allowed, StatusTransitions.IsAllowed(from, to));
    }

    [Fact]
    public void Move_NotAllowed_ChangesNothingAndWritesNoHistory()
    {
        using var ctx = TestSupport.NewInMemoryContext();
        var scenario = new Scenario(ctx);

        var result = StatusTransitions.Move(ctx, scenario.Request, RequestStatus.Completed, scenario.Customer.Id, DateTime.UtcNow);
        ctx.SaveChanges();

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(RequestStatus.Created, scenario.Request.Status);
        Assert.Empty(ctx.StatusHistories);
    }

    [Fact]
    public void Move_Allowed_RecordsWhoWhenFromAndTo()
    {
        using var ctx = TestSupport.NewInMemoryContext();
        var scenario = new Scenario(ctx);
        var at = DateTime.UtcNow;

        var result = StatusTransitions.Move(ctx, scenario.Request, RequestStatus.Cancelled, scenario.Customer.Id, at, note: "No longer needed");
        ctx.SaveChanges();

        Assert.True(result.IsSuccess);
        var history = Assert.Single(ctx.StatusHistories);
        Assert.Equal(scenario.Request.Id, history.ServiceRequestId);
        Assert.Equal(scenario.Customer.Id, history.ChangedByUserId);
        Assert.Equal(at, history.ChangedAtUtc);
        Assert.Equal(RequestStatus.Created, history.PreviousStatus);
        Assert.Equal(RequestStatus.Cancelled, history.NewStatus);
        Assert.Equal("No longer needed", history.Note);
    }
}
