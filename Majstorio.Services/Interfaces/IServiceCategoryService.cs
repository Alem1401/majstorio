using Majstorio.Model.Requests.ServiceCategory;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.ServiceCategory;
using Majstorio.Model.Results;

public interface IServiceCategoryService
{
    Task<Result<PagedResponse<ServiceCategoryResponse>>> GetServiceCategoriesAsync(ServiceCategorySearchRequest search);

    Task<Result<ServiceCategoryResponse>> GetServiceCategoryByIdAsync(int id);

    Task<Result<ServiceCategoryResponse>> CreateServiceCategoryAsync(ServiceCategoryCreateRequest request);

    Task<Result> UpdateServiceCategoryAsync(ServiceCategoryUpdateRequest request);

    Task<Result> DeleteServiceCategoryAsync(ServiceCategoryDeleteRequest request);
}
