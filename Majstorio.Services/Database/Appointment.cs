using Majstorio.Services.Enums;

namespace Majstorio.Services.Database;


public class Appointment : BaseEntity
{
   
    public int OfferId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    public Offer Offer { get; set; } = null!;
}
