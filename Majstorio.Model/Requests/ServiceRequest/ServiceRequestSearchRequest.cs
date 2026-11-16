namespace Majstorio.Model.Requests.ServiceRequest;

public class ServiceRequestSearchRequest : BaseSearchRequest
{
    // 0 = Created, 1 = OfferAccepted, 2 = AppointmentConfirmed, 3 = Completed, 4 = Cancelled.
    public int? Status { get; set; }
    public int? CityId { get; set; }
    public int? ServiceCategoryId { get; set; }
    // Period filter on the creation time.
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
