using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.RequestStateMachine;

// The only place that changes ServiceRequest.Status and Appointment.Status.
// It checks that the move is allowed and adds the StatusHistory row (who, when, from, to).
// The caller saves the changes, so the status and its history row are written together.
public static class StatusTransitions
{
    // Created -> OfferAccepted -> AppointmentConfirmed -> Completed, with Cancelled possible until completion.
    private static readonly Dictionary<RequestStatus, RequestStatus[]> RequestMoves = new()
    {
        [RequestStatus.Created] = [RequestStatus.OfferAccepted, RequestStatus.Cancelled],
        [RequestStatus.OfferAccepted] = [RequestStatus.AppointmentConfirmed, RequestStatus.Cancelled],
        [RequestStatus.AppointmentConfirmed] = [RequestStatus.Completed, RequestStatus.Cancelled],
        [RequestStatus.Completed] = [],
        [RequestStatus.Cancelled] = []
    };

    // Pending -> Confirmed -> Completed, with Cancelled possible until completion.
    private static readonly Dictionary<AppointmentStatus, AppointmentStatus[]> AppointmentMoves = new()
    {
        [AppointmentStatus.Pending] = [AppointmentStatus.Confirmed, AppointmentStatus.Cancelled],
        [AppointmentStatus.Confirmed] = [AppointmentStatus.Completed, AppointmentStatus.Cancelled],
        [AppointmentStatus.Completed] = [],
        [AppointmentStatus.Cancelled] = []
    };

    public static bool IsAllowed(RequestStatus from, RequestStatus to) => RequestMoves[from].Contains(to);

    // A new appointment (no previous status) always starts as Pending.
    public static bool IsAllowed(AppointmentStatus? from, AppointmentStatus to) =>
        from == null ? to == AppointmentStatus.Pending : AppointmentMoves[from.Value].Contains(to);

    // Call inside a transaction before reading the request. The row stays locked until the transaction ends,
    // so two status changes of the same request run one after another and the second one sees the first.
    public static async Task LockRequestAsync(MajstorioDbContext ctx, int serviceRequestId)
    {
        // The in-memory provider used by unit tests has no SQL and no parallel transactions.
        if (ctx.Database.IsSqlServer())
            await ctx.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT Id FROM ServiceRequests WITH (UPDLOCK, ROWLOCK) WHERE Id = {serviceRequestId}");
    }

    // Writes the first history row of a new request: no previous status, new status Created.
    public static void RecordCreated(MajstorioDbContext ctx, ServiceRequest request, int userId, DateTime atUtc)
    {
        request.Status = RequestStatus.Created;
        ctx.StatusHistories.Add(new StatusHistory
        {
            ServiceRequest = request, ChangedByUserId = userId, ChangedAtUtc = atUtc,
            PreviousStatus = null, NewStatus = RequestStatus.Created
        });
    }

    // Moves the request and, when given, its appointment. An appointment the context does not know yet is new.
    // Nothing is changed when either move is not allowed.
    public static Result Move(
        MajstorioDbContext ctx, ServiceRequest request, RequestStatus newStatus, int userId, DateTime atUtc,
        Appointment? appointment = null, AppointmentStatus? newAppointmentStatus = null, string? note = null)
    {
        if (!IsAllowed(request.Status, newStatus))
            return Result.Failure(ErrorType.Conflict, $"A request cannot move from {request.Status} to {newStatus}.");

        var history = new StatusHistory
        {
            ServiceRequest = request, ChangedByUserId = userId, ChangedAtUtc = atUtc,
            PreviousStatus = request.Status, NewStatus = newStatus, Note = note
        };

        if (appointment != null && newAppointmentStatus.HasValue)
        {
            var isNew = ctx.Entry(appointment).State == EntityState.Detached;
            AppointmentStatus? previous = isNew ? null : appointment.Status;
            if (!IsAllowed(previous, newAppointmentStatus.Value))
                return Result.Failure(ErrorType.Conflict,
                    $"An appointment cannot move from {previous?.ToString() ?? "new"} to {newAppointmentStatus}.");

            appointment.Status = newAppointmentStatus.Value;
            // Setting the navigation also inserts a new appointment together with its history row.
            history.Appointment = appointment;
            history.PreviousAppointmentStatus = previous;
            history.NewAppointmentStatus = newAppointmentStatus;
        }

        request.Status = newStatus;
        ctx.StatusHistories.Add(history);
        return Result.Success();
    }
}
