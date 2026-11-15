using Majstorio.Model.Requests.Appointment;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Appointment;
using Majstorio.Model.Results;

public interface IAppointmentService
{
    Task<Result<PagedResponse<AppointmentResponse>>> GetAppointmentsAsync(AppointmentSearchRequest search);
    Task<Result<AppointmentResponse>> GetAppointmentByIdAsync(int id);
    Task<Result> CreateAppointmentAsync(AppointmentCreateRequest request);
    Task<Result> UpdateAppointmentAsync(AppointmentUpdateRequest request);
    Task<Result> DeleteAppointmentAsync(AppointmentDeleteRequest request);
}
