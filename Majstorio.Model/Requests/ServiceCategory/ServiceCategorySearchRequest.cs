namespace Majstorio.Model.Requests.ServiceCategory;

public class ServiceCategorySearchRequest : BaseSearchRequest
{
    // Matches categories whose name contains this text.
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
}
