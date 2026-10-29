using Majstorio.Model.Requests.ServiceCategory;
using Majstorio.Model.Responses.ServiceCategory;
using Majstorio.Model.Results;

public interface IServiceCategoryService
{
    Task<Result> CreateServiceCategoryAsync(ServiceCategoryCreateRequest request);
    Task<Result> UpdateServiceCategoryAsync(ServiceCategoryUpdateRequest request);
    Task<Result> DeleteServiceCategoryAsync(ServiceCategoryDeleteRequest request);
    Task<Result<List<ServiceCategoryResponse>>> GetAllServiceCategoriesAsync();
    Task<Result<ServiceCategoryResponse>> GetServiceCategoryByIdAsync(int id);
}
