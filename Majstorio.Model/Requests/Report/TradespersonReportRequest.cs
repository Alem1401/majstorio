namespace Majstorio.Model.Requests.Report;

// Filters of the "tradesperson performance" report; every filter is optional.
public class TradespersonReportRequest
{
    // Offers and reviews created on or after this moment.
    public DateTime? FromUtc { get; set; }
    // Offers and reviews created before this moment.
    public DateTime? ToUtc { get; set; }
    public int? CityId { get; set; }
}
