namespace Majstorio.Model.Requests.Payment;

public class PaymentSearchRequest : BaseSearchRequest
{
    // 0 = Pending, 1 = Paid, 2 = Failed, 3 = Cancelled.
    public int? Status { get; set; }
}
