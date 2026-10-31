namespace Majstorio.Model.Responses.Appointment;

public class AppointmentResponse
{
    public int Id { get; set; }
    public int OfferId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    // 0 = Pending, 1 = Confirmed, 2 = Cancelled, 3 = Completed.
    public int Status { get; set; }
}
