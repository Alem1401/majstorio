using Majstorio.Model.Requests.Notification;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Mapping;
using Majstorio.Services.Services;
using Mapster;
using MapsterMapper;
using Xunit;

namespace Majstorio.Services.Tests;

public class NotificationServiceTests : IDisposable
{
    private readonly MajstorioDbContext _ctx;
    private readonly NotificationService _service;
    private readonly ApplicationUser _user;
    private readonly ApplicationUser _otherUser;

    public NotificationServiceTests()
    {
        _ctx = TestSupport.NewInMemoryContext();
        var config = new TypeAdapterConfig();
        new NotificationMappingProfile().Register(config);
        _service = new NotificationService(_ctx, new Mapper(config));

        _user = TestSupport.NewUser();
        _otherUser = TestSupport.NewUser();
        _ctx.AddRange(_user, _otherUser);
        _ctx.SaveChanges();
    }

    public void Dispose() => _ctx.Dispose();

    private Notification AddNotification(ApplicationUser user, string title, bool isRead = false, int minutesAgo = 0)
    {
        var notification = new Notification
        {
            UserId = user.Id, EventId = Guid.NewGuid(), Title = title, Message = "Text",
            IsRead = isRead, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-minutesAgo)
        };
        _ctx.Notifications.Add(notification);
        _ctx.SaveChanges();
        return notification;
    }

    [Fact]
    public async Task GetNotifications_ReturnsOnlyOwnRowsNewestFirstWithPaging()
    {
        AddNotification(_user, "Old", minutesAgo: 30);
        AddNotification(_user, "Middle", minutesAgo: 20);
        AddNotification(_user, "New", minutesAgo: 10);
        AddNotification(_otherUser, "Someone else's");

        var result = await _service.GetNotificationsAsync(TestSupport.As(_user),
            new NotificationSearchRequest { Page = 1, PageSize = 2 });

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.TotalCount);
        Assert.Equal(new[] { "New", "Middle" }, result.Value.Items.Select(item => item.Title));
    }

    [Fact]
    public async Task GetNotifications_FiltersByIsRead()
    {
        AddNotification(_user, "Unread");
        AddNotification(_user, "Read", isRead: true);

        var result = await _service.GetNotificationsAsync(TestSupport.As(_user),
            new NotificationSearchRequest { IsRead = false });

        Assert.True(result.IsSuccess);
        Assert.Equal("Unread", Assert.Single(result.Value!.Items).Title);
    }

    [Fact]
    public async Task MarkAsRead_MarksOwnNotification()
    {
        var notification = AddNotification(_user, "Unread");

        var result = await _service.MarkAsReadAsync(TestSupport.As(_user), notification.Id);

        Assert.True(result.IsSuccess);
        Assert.True(_ctx.Notifications.Single(row => row.Id == notification.Id).IsRead);
    }

    [Fact]
    public async Task MarkAsRead_ReturnsNotFoundForAnotherUsersNotification()
    {
        var notification = AddNotification(_otherUser, "Not yours");

        var result = await _service.MarkAsReadAsync(TestSupport.As(_user), notification.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.False(_ctx.Notifications.Single(row => row.Id == notification.Id).IsRead);
    }

    [Fact]
    public async Task MarkAllAsRead_ChangesOnlyTheCallersNotifications()
    {
        AddNotification(_user, "First");
        AddNotification(_user, "Second");
        var other = AddNotification(_otherUser, "Not yours");

        var result = await _service.MarkAllAsReadAsync(TestSupport.As(_user));

        Assert.True(result.IsSuccess);
        Assert.All(_ctx.Notifications.Where(row => row.UserId == _user.Id), row => Assert.True(row.IsRead));
        Assert.False(_ctx.Notifications.Single(row => row.Id == other.Id).IsRead);
    }
}
