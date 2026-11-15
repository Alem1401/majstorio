namespace Majstorio.Model.Requests.Appointment;

public class AppointmentSearchRequest : BaseSearchRequest
{
    // 0 = Pending, 1 = Confirmed, 2 = Cancelled, 3 = Completed.
    public int? Status { get; set; }
}
