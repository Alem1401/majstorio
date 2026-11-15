namespace Majstorio.Model.Requests.Offer;

public class OfferSearchRequest : BaseSearchRequest
{
    // 0 = Pending, 1 = Accepted, 2 = Rejected, 3 = Withdrawn, 4 = Closed.
    public int? Status { get; set; }
}
