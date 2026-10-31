using Majstorio.Model.Requests.Appointment;
using Majstorio.Model.Responses.Appointment;
using Majstorio.Model.Results;

public interface IAppointmentService
{
    Task<Result> CreateAppointmentAsync(AppointmentCreateRequest request);
    Task<Result> UpdateAppointmentAsync(AppointmentUpdateRequest request);
    Task<Result> DeleteAppointmentAsync(AppointmentDeleteRequest request);
    Task<Result<List<AppointmentResponse>>> GetAllAppointmentsAsync();
    Task<Result<AppointmentResponse>> GetAppointmentByIdAsync(int id);
}
