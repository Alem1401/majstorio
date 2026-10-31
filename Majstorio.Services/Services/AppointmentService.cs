using Majstorio.Model.Requests.Appointment;
using Majstorio.Model.Responses.Appointment;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

public class AppointmentService : IAppointmentService
{
    private readonly MajstorioDbContext _ctx;
    private readonly IMapper _mapper;

    public AppointmentService(MajstorioDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<Result> CreateAppointmentAsync(AppointmentCreateRequest request)
    {
        var validation = await ValidateAppointmentAsync(
            request.OfferId, request.StartUtc, request.EndUtc, AppointmentStatus.Pending);
        if (!validation.IsSuccess)
            return validation;

        await _ctx.Appointments.AddAsync(_mapper.Map<Appointment>(request));
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateAppointmentAsync(AppointmentUpdateRequest request)
    {
        var appointment = await _ctx.Appointments.FirstOrDefaultAsync(appointment => appointment.Id == request.Id);
        if (appointment == null)
            return Result.Failure(ErrorType.NotFound, "Appointment not found.");

        var status = (AppointmentStatus)request.Status;
        var validation = await ValidateAppointmentAsync(
            request.OfferId, request.StartUtc, request.EndUtc, status, request.Id);
        if (!validation.IsSuccess)
            return validation;

        var scheduleChanged = appointment.OfferId != request.OfferId
            || appointment.StartUtc != request.StartUtc || appointment.EndUtc != request.EndUtc;

        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
        {
            if (scheduleChanged || appointment.Status != status)
                return Result.Failure(ErrorType.Conflict, "Cancelled or completed appointments cannot be changed.");
        }

        if (appointment.Status == AppointmentStatus.Confirmed && status == AppointmentStatus.Pending)
            return Result.Failure(ErrorType.Conflict, "A confirmed appointment cannot return to pending.");

        if (status == AppointmentStatus.Completed && appointment.Status != AppointmentStatus.Completed)
        {
            if (appointment.Status != AppointmentStatus.Confirmed || scheduleChanged)
                return Result.Failure(ErrorType.Conflict, "Only a confirmed appointment can be completed without changing its schedule.");

            if (request.EndUtc > DateTime.UtcNow)
                return Result.Failure(ErrorType.Validation, "An appointment cannot be completed before its end.");
        }

        if (scheduleChanged && status is AppointmentStatus.Pending or AppointmentStatus.Confirmed
            && request.StartUtc <= DateTime.UtcNow)
            return Result.Failure(ErrorType.Validation, "Appointment start must be in the future when rescheduling.");

        _mapper.Map(request, appointment);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteAppointmentAsync(AppointmentDeleteRequest request)
    {
        var appointment = await _ctx.Appointments.FirstOrDefaultAsync(appointment => appointment.Id == request.Id);
        if (appointment == null)
            return Result.Failure(ErrorType.NotFound, "Appointment not found.");

        _ctx.Appointments.Remove(appointment);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<AppointmentResponse>>> GetAllAppointmentsAsync()
    {
        var appointments = await _ctx.Appointments.ToListAsync();
        return Result<List<AppointmentResponse>>.Success(
            _mapper.Map<List<AppointmentResponse>>(appointments));
    }

    public async Task<Result<AppointmentResponse>> GetAppointmentByIdAsync(int id)
    {
        var appointment = await _ctx.Appointments.FirstOrDefaultAsync(appointment => appointment.Id == id);
        if (appointment == null)
            return Result<AppointmentResponse>.Failure(ErrorType.NotFound, "Appointment not found.");

        return Result<AppointmentResponse>.Success(_mapper.Map<AppointmentResponse>(appointment));
    }

    private async Task<Result> ValidateAppointmentAsync(
        int offerId, DateTime startUtc, DateTime endUtc, AppointmentStatus status, int? appointmentId = null)
    {
        var offer = await _ctx.Offers
            .Include(offer => offer.ServiceRequest)
            .FirstOrDefaultAsync(offer => offer.Id == offerId);
        if (offer == null)
            return Result.Failure(ErrorType.NotFound, "Offer not found.");

        if (offer.Status != OfferStatus.Accepted)
            return Result.Failure(ErrorType.Conflict, "Only an accepted offer can have an appointment.");

        if (await _ctx.Appointments.AnyAsync(appointment =>
            appointment.OfferId == offerId && appointment.Id != appointmentId))
            return Result.Failure(ErrorType.Conflict, "An appointment already exists for this offer.");

        if (status is AppointmentStatus.Pending or AppointmentStatus.Confirmed
            && await _ctx.Appointments.AnyAsync(appointment =>
                appointment.Id != appointmentId
                && (appointment.Status == AppointmentStatus.Pending || appointment.Status == AppointmentStatus.Confirmed)
                && appointment.StartUtc < endUtc && appointment.EndUtc > startUtc
                && (appointment.Offer.TradespersonProfileId == offer.TradespersonProfileId
                    || appointment.Offer.ServiceRequest.CustomerId == offer.ServiceRequest.CustomerId)))
            return Result.Failure(ErrorType.Conflict, "The customer or tradesperson already has an overlapping appointment.");

        return Result.Success();
    }
}
