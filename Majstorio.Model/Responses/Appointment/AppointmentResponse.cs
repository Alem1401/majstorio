namespace Majstorio.Model.Responses.Appointment;

public class AppointmentResponse
{
    public int Id { get; set; }
    public int OfferId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string TradespersonName { get; set; } = string.Empty;
    public string ServiceCategoryName { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    // 0 = Pending, 1 = Confirmed, 2 = Cancelled, 3 = Completed.
    public int Status { get; set; }
    // True once PayPal confirmed the payment; the app then hides the pay button.
    public bool IsPaid { get; set; }
    public bool IsRefunded { get; set; }
}
