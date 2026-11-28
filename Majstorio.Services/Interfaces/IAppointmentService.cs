using Majstorio.Model.Requests.Appointment;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Appointment;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;

public interface IAppointmentService
{
    // Administrative read-only overview of all appointments.
    Task<Result<PagedResponse<AppointmentResponse>>> GetAppointmentsAsync(AppointmentSearchRequest search);

    // Appointments of the user's own requests, or of the jobs assigned to them as a tradesperson.
    Task<Result<PagedResponse<AppointmentResponse>>> GetMyAppointmentsAsync(CurrentUser user, AppointmentSearchRequest search);

    // The assigned tradesperson marks the job as done.
    Task<Result> CompleteAppointmentAsync(CurrentUser user, int id);
}
