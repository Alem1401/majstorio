using Majstorio.Services.Enums;

namespace Majstorio.Services.Database;

// Scheduled work from an accepted offer; customer and tradesperson are reached through that offer.
public class Appointment : BaseEntity
{
    // Links to Offer.Id; an offer has zero or one appointment in the intended design.
    public int OfferId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    public Offer Offer { get; set; } = null!;
}
