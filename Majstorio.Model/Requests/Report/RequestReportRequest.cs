namespace Majstorio.Model.Requests.Report;

// Filters of the "service requests" report; every filter is optional.
public class RequestReportRequest
{
    // Requests created on or after this moment.
    public DateTime? FromUtc { get; set; }
    // Requests created before this moment.
    public DateTime? ToUtc { get; set; }
    public int? CityId { get; set; }
    public int? ServiceCategoryId { get; set; }
    // 0 = Created, 1 = OfferAccepted, 2 = AppointmentConfirmed, 3 = Completed, 4 = Cancelled.
    public int? Status { get; set; }
}
