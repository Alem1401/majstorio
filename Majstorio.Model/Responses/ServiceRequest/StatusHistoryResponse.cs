namespace Majstorio.Model.Responses.ServiceRequest;

// One recorded transition: who changed the status, when, from what and to what.
public class StatusHistoryResponse
{
    public int Id { get; set; }
    public int ChangedByUserId { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; }
    // Request status; null on the first entry, when the request was created.
    public int? PreviousStatus { get; set; }
    public int NewStatus { get; set; }
    // Appointment status, when the same change also created or moved the appointment.
    public int? PreviousAppointmentStatus { get; set; }
    public int? NewAppointmentStatus { get; set; }
    public string? Note { get; set; }
}
