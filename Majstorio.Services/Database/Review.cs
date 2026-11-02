namespace Majstorio.Services.Database;

// A customer's review of completed work.
public class Review : BaseEntity
{
    // Links to Appointment.Id; unique, so an appointment has zero or one review.
    public int AppointmentId { get; set; }
    // Copied from the appointment's offer when the review is created, so ratings per
    // tradesperson (search, recommendation, reports) are a single query.
    public int TradespersonProfileId { get; set; }
    // Copied from the appointment's service request; the reviewing customer.
    public int CustomerId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }

    public Appointment Appointment { get; set; } = null!;
    public TradespersonProfile TradespersonProfile { get; set; } = null!;
    public ApplicationUser Customer { get; set; } = null!;
}
