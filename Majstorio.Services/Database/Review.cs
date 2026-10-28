namespace Majstorio.Services.Database;

// A review of completed work; its customer and tradesperson are derived through the appointment.
public class Review : BaseEntity
{
    // Links to Appointment.Id; an appointment has zero or one customer review.
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }

    public Appointment Appointment { get; set; } = null!;
}
