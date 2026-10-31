namespace Majstorio.Model.Requests.Appointment;

public class AppointmentCreateRequest
{
    public int OfferId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
}
