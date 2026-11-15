namespace Majstorio.Model.Responses.Report;

// One line of the "service requests" report: how many requests a city and category have in one status.
public class RequestReportRowResponse
{
    public string CityName { get; set; } = string.Empty;
    public string ServiceCategoryName { get; set; } = string.Empty;
    // 0 = Created, 1 = OfferAccepted, 2 = AppointmentConfirmed, 3 = Completed, 4 = Cancelled.
    public int Status { get; set; }
    public int RequestCount { get; set; }
}
