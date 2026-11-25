using Majstorio.Model.Requests.Notification;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Notification;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;
using Majstorio.Services.Database;
using Majstorio.Services.Extensions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

// Notifications are written by the Worker; here a user only reads their own and marks them read.
public class NotificationService : INotificationService
{
    private const string NotFoundMessage = "Notification not found.";

    private readonly MajstorioDbContext _ctx;
    private readonly IMapper _mapper;

    public NotificationService(MajstorioDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<Result<PagedResponse<NotificationResponse>>> GetNotificationsAsync(CurrentUser user, NotificationSearchRequest search)
    {
        var query = _ctx.Notifications.AsNoTracking()
            .Where(notification => notification.UserId == user.Id);

        if (search.IsRead.HasValue)
            query = query.Where(notification => notification.IsRead == search.IsRead.Value);

        var page = await query
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ThenByDescending(notification => notification.Id)
            .ToPagedAsync(search, notifications => _mapper.Map<List<NotificationResponse>>(notifications));

        return Result<PagedResponse<NotificationResponse>>.Success(page);
    }

    public async Task<Result> MarkAsReadAsync(CurrentUser user, int id)
    {
        // Another user's notification is reported as not found, so ids cannot be probed.
        var notification = await _ctx.Notifications
            .FirstOrDefaultAsync(notification => notification.Id == id && notification.UserId == user.Id);
        if (notification == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        notification.IsRead = true;
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> MarkAllAsReadAsync(CurrentUser user)
    {
        var unread = await _ctx.Notifications
            .Where(notification => notification.UserId == user.Id && !notification.IsRead)
            .ToListAsync();

        foreach (var notification in unread)
            notification.IsRead = true;

        await _ctx.SaveChangesAsync();
        return Result.Success();
    }
}
