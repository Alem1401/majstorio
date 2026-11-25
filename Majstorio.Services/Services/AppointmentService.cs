using Majstorio.Model.Messages;
using Majstorio.Model.Requests.Appointment;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Appointment;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Extensions;
using Majstorio.Services.RequestStateMachine;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

// Appointments are created when an offer is accepted (OfferService) and cancelled with their request
// (ServiceRequestService); this service lists them and completes them.
public class AppointmentService : IAppointmentService
{
    private readonly MajstorioDbContext _ctx;
    private readonly IMapper _mapper;
    private readonly IMessagePublisher _publisher;

    public AppointmentService(MajstorioDbContext ctx, IMapper mapper, IMessagePublisher publisher)
    {
        _ctx = ctx;
        _mapper = mapper;
        _publisher = publisher;
    }

    public async Task<Result> CompleteAppointmentAsync(CurrentUser user, int id)
    {
        await using var transaction = await _ctx.Database.BeginTransactionAsync();

        var serviceRequestId = await _ctx.Appointments.Where(appointment => appointment.Id == id)
            .Select(appointment => (int?)appointment.Offer.ServiceRequestId).FirstOrDefaultAsync();
        if (serviceRequestId == null)
            return Result.Failure(ErrorType.NotFound, "Appointment not found.");

        // Locked first, so completing and cancelling the same job cannot overwrite each other.
        await StatusTransitions.LockRequestAsync(_ctx, serviceRequestId.Value);

        var appointment = await _ctx.Appointments
            .Include(appointment => appointment.Offer.ServiceRequest)
            .Include(appointment => appointment.Offer.TradespersonProfile)
            .FirstAsync(appointment => appointment.Id == id);

        if (appointment.Offer.TradespersonProfile.UserId != user.Id)
            return Result.Failure(ErrorType.Forbidden, "Only the assigned tradesperson can mark the job as completed.");

        if (appointment.Status == AppointmentStatus.Confirmed && appointment.StartUtc > DateTime.UtcNow)
            return Result.Failure(ErrorType.Conflict, "A job cannot be completed before its appointment starts.");

        var move = StatusTransitions.Move(_ctx, appointment.Offer.ServiceRequest, RequestStatus.Completed,
            user.Id, DateTime.UtcNow, appointment, AppointmentStatus.Completed);
        if (!move.IsSuccess)
            return move;

        await _ctx.SaveChangesAsync();
        await transaction.CommitAsync();

        await _publisher.PublishAsync(new NotificationMessage
        {
            UserId = appointment.Offer.ServiceRequest.CustomerId,
            ServiceRequestId = appointment.Offer.ServiceRequestId,
            Title = "Job completed",
            Message = "The tradesperson marked your job as completed."
        });
        return Result.Success();
    }

    public Task<Result<PagedResponse<AppointmentResponse>>> GetAppointmentsAsync(AppointmentSearchRequest search)
        => GetPageAsync(AppointmentsWithNames(), search);

    public Task<Result<PagedResponse<AppointmentResponse>>> GetMyAppointmentsAsync(CurrentUser user, AppointmentSearchRequest search)
        => GetPageAsync(AppointmentsWithNames().Where(appointment =>
            appointment.Offer.TradespersonProfile.UserId == user.Id
            || appointment.Offer.ServiceRequest.CustomerId == user.Id), search);

    public async Task<Result<AppointmentResponse>> GetAppointmentByIdAsync(int id)
    {
        var appointment = await AppointmentsWithNames().FirstOrDefaultAsync(appointment => appointment.Id == id);
        if (appointment == null)
            return Result<AppointmentResponse>.Failure(ErrorType.NotFound, "Appointment not found.");

        return Result<AppointmentResponse>.Success(ToResponse(appointment));
    }

    private async Task<Result<PagedResponse<AppointmentResponse>>> GetPageAsync(
        IQueryable<Appointment> query, AppointmentSearchRequest search)
    {
        if (search.Status.HasValue)
        {
            var status = (AppointmentStatus)search.Status.Value;
            query = query.Where(appointment => appointment.Status == status);
        }

        var page = await query
            .OrderByDescending(appointment => appointment.StartUtc)
            .ThenByDescending(appointment => appointment.Id)
            .ToPagedAsync(search, appointments => appointments.Select(ToResponse).ToList());

        return Result<PagedResponse<AppointmentResponse>>.Success(page);
    }

    // Loads the people and the category with the appointments in one query, so lists can show names instead of ids.
    private IQueryable<Appointment> AppointmentsWithNames() => _ctx.Appointments.AsNoTracking()
        .Include(appointment => appointment.Offer.ServiceRequest.Customer)
        .Include(appointment => appointment.Offer.ServiceRequest.ServiceCategory)
        .Include(appointment => appointment.Offer.TradespersonProfile.User);

    private AppointmentResponse ToResponse(Appointment appointment)
    {
        var response = _mapper.Map<AppointmentResponse>(appointment);
        var offer = appointment.Offer;
        response.CustomerName = PersonNames.Full(offer?.ServiceRequest?.Customer);
        response.TradespersonName = PersonNames.Full(offer?.TradespersonProfile?.User);
        response.ServiceCategoryName = offer?.ServiceRequest?.ServiceCategory?.Name ?? string.Empty;
        return response;
    }
}
