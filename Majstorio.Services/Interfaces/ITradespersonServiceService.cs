using Majstorio.Model.Requests.TradespersonService;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.TradespersonProfile;
using Majstorio.Model.Results;

public interface ITradespersonServiceService
{
    Task<Result<PagedResponse<TradespersonServiceResponse>>> GetServicesAsync(TradespersonServiceSearchRequest search);

    Task<Result<TradespersonServiceResponse>> GetServiceByIdAsync(int id);

    Task<Result<TradespersonServiceResponse>> CreateServiceAsync(TradespersonServiceCreateRequest request);

    Task<Result> UpdateServiceAsync(TradespersonServiceUpdateRequest request);

    Task<Result> DeleteServiceAsync(TradespersonServiceDeleteRequest request);
}
