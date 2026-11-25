using Majstorio.Model.Requests.ServiceRequest;
using Majstorio.Services.Database;
using Majstorio.Services.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

// Checks who gets a message (and so a notification) after each status change.
public class NotificationPublishingTests : IDisposable
{
    private readonly MajstorioDbContext _ctx = TestSupport.NewInMemoryContext();
    private readonly Scenario _s;
    private readonly FakeMessagePublisher _publisher = new();

    public NotificationPublishingTests() => _s = new Scenario(_ctx);

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task AcceptOffer_TellsTheWinnerAndTheTradespeopleWhoseOffersWereClosed()
    {
        var winner = _s.AddOffer();
        var loser = _s.AddOffer(_s.AddProfile());

        Assert.True((await Offers().AcceptOfferAsync(TestSupport.As(_s.Customer), winner.Id)).IsSuccess);

        Assert.Equal(2, _publisher.Sent.Count);
        Assert.Contains(_publisher.Sent, message => message.UserId == _s.Profile.UserId && message.Title == "Offer accepted");
        Assert.Contains(_publisher.Sent, message => message.UserId == loser.TradespersonProfile.UserId
            && message.Title == "Offer not accepted");
    }

    [Fact]
    public async Task AcceptOffer_ThatFails_SendsNothing()
    {
        var offer = _s.AddOffer();

        var result = await Offers().AcceptOfferAsync(TestSupport.As(_s.AddProfile().User), offer.Id);

        Assert.False(result.IsSuccess);
        Assert.Empty(_publisher.Sent);
    }

    [Fact]
    public async Task CompleteAppointment_TellsTheCustomer()
    {
        var offer = _s.AddOffer();
        await Offers().AcceptOfferAsync(TestSupport.As(_s.Customer), offer.Id);
        var appointment = await _ctx.Appointments.SingleAsync();
        appointment.StartUtc = DateTime.UtcNow.AddHours(-2);
        appointment.EndUtc = DateTime.UtcNow.AddHours(-1);
        await _ctx.SaveChangesAsync();
        _publisher.Sent.Clear();

        var appointments = new AppointmentService(_ctx, TestSupport.NewMapper(), _publisher);
        Assert.True((await appointments.CompleteAppointmentAsync(TestSupport.As(_s.Profile.User), appointment.Id)).IsSuccess);

        var message = Assert.Single(_publisher.Sent);
        Assert.Equal(_s.Customer.Id, message.UserId);
        Assert.Equal(_s.Request.Id, message.ServiceRequestId);
    }

    [Fact]
    public async Task CancelRequest_TellsEveryoneInvolvedExceptWhoCancelled()
    {
        _s.AddOffer();
        var other = _s.AddOffer(_s.AddProfile());

        var requests = new ServiceRequestService(_ctx, TestSupport.NewMapper(), _publisher);
        var result = await requests.CancelServiceRequestAsync(TestSupport.As(_s.Customer),
            new ServiceRequestCancelRequest { Id = _s.Request.Id, Reason = "No longer needed." });

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new[] { _s.Profile.UserId, other.TradespersonProfile.UserId }.OrderBy(id => id),
            _publisher.Sent.Select(message => message.UserId).OrderBy(id => id));
    }

    private OfferService Offers() => new(_ctx, TestSupport.NewMapper(), _publisher);
}
