namespace Majstorio.Model.Responses.Report;

// One line of the "tradesperson performance" report.
public class TradespersonReportRowResponse
{
    public string TradespersonName { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public int OfferCount { get; set; }
    public int CompletedJobCount { get; set; }
    // Null when the tradesperson has no reviews in the period.
    public double? AverageRating { get; set; }
}
