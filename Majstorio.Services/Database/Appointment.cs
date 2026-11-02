using Majstorio.Services.Enums;

namespace Majstorio.Services.Database;


public class Appointment : BaseEntity
{
    // Links to Offer.Id; unique, so an offer has at most one appointment.
    public int OfferId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    public Offer Offer { get; set; } = null!;

    // A job can have many payment attempts, but at most one customer review.
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public Review? Review { get; set; }
    public ICollection<StatusHistory> StatusHistories { get; set; } = new List<StatusHistory>();
}
