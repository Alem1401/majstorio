using Majstorio.Model.Requests.ServiceCategory;
using Majstorio.Model.Responses.ServiceCategory;

public interface IServiceCategoryService
{
    Task<bool> CreateServiceCategoryAsync(ServiceCategoryCreateRequest request);
    Task<bool> UpdateServiceCategoryAsync(ServiceCategoryUpdateRequest request);
    Task<bool> DeleteServiceCategoryAsync(ServiceCategoryDeleteRequest request);
    Task<List<ServiceCategoryResponse>> GetAllServiceCategoriesAsync();
    Task<ServiceCategoryResponse?> GetServiceCategoryByIdAsync(int id);
}
