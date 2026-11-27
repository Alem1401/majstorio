namespace Majstorio.Model.Requests.Payment;

// Starts the PayPal payment of a completed job. The amount is never sent: the API takes it from the accepted offer.
public class PaymentCreateRequest
{
    public int AppointmentId { get; set; }
}
