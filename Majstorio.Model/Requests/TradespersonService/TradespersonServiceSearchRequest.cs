namespace Majstorio.Model.Requests.TradespersonService;

public class TradespersonServiceSearchRequest : BaseSearchRequest
{
    public int? TradespersonProfileId { get; set; }
    // Matches services whose name contains this text.
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
}
