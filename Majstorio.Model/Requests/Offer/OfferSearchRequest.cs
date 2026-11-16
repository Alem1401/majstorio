namespace Majstorio.Model.Requests.Offer;

public class OfferSearchRequest : BaseSearchRequest
{
    // 0 = Pending, 1 = Accepted, 2 = Rejected, 3 = Withdrawn, 4 = Closed.
    public int? Status { get; set; }
    // Only the offers of one request, for example to compare them before accepting.
    public int? ServiceRequestId { get; set; }
}
