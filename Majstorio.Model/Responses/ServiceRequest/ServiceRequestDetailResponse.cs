using Majstorio.Model.Responses.Appointment;
using Majstorio.Model.Responses.Offer;

namespace Majstorio.Model.Responses.ServiceRequest;

// A request with everything the detail screen shows: its offers, the appointment and the status history.
public class ServiceRequestDetailResponse : ServiceRequestResponse
{
    // The customer and the administrator see every offer; a tradesperson sees only their own.
    public List<OfferResponse> Offers { get; set; } = [];
    public AppointmentResponse? Appointment { get; set; }
    public List<StatusHistoryResponse> StatusHistory { get; set; } = [];
}
