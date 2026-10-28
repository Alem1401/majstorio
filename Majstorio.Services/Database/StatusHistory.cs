using Majstorio.Services.Enums;

namespace Majstorio.Services.Database;

// Records request changes and, when relevant, the same operation's appointment change.
public class StatusHistory : BaseEntity
{
    // Links to ServiceRequest.Id; one request can have many history entries.
    public int ServiceRequestId { get; set; }
    // Optional Appointment.Id; the service checks that it belongs to this request.
    public int? AppointmentId { get; set; }
    // Links to ApplicationUser.Id; one account can make many recorded changes.
    public int ChangedByUserId { get; set; }
    // Null is useful for the first entry, when the request has no previous state.
    public RequestStatus? PreviousStatus { get; set; }
    public RequestStatus NewStatus { get; set; }
    // Optional previous appointment state, including null when first scheduled.
    public AppointmentStatus? PreviousAppointmentStatus { get; set; }
    // Set when this row also records an appointment's creation or status change.
    public AppointmentStatus? NewAppointmentStatus { get; set; }
    public string? Note { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;

    public ServiceRequest ServiceRequest { get; set; } = null!;
    public Appointment? Appointment { get; set; }
    public ApplicationUser ChangedByUser { get; set; } = null!;
}
