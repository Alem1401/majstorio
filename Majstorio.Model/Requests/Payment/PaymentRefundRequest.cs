using System.Text.Json.Serialization;

namespace Majstorio.Model.Requests.Payment;

// A full refund of the captured amount. The amount is never sent: the API refunds what PayPal actually captured.
public class PaymentRefundRequest
{
    // Filled in by the controller from the route (POST /Payments/{id}/refund).
    // [JsonIgnore]: an id sent in the body is never read, so the client cannot pick another record.
    [JsonIgnore]
    public int Id { get; set; }
    public string Reason { get; set; } = string.Empty;
}
