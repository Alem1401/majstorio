namespace Majstorio.Model.Requests.ServiceCategory;

// The category id comes from the route (PUT /ServiceCategories/{id}), not from the body.
public class ServiceCategoryUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
