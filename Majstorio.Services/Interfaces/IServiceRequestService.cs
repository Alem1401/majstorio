using Majstorio.Model.Requests.ServiceRequest;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.ServiceRequest;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;

public interface IServiceRequestService
{
    // A customer gets their own requests, a tradesperson the ones matching their category and city,
    // an administrator all of them.
    Task<Result<PagedResponse<ServiceRequestResponse>>> GetServiceRequestsAsync(CurrentUser user, ServiceRequestSearchRequest search);
    Task<Result<ServiceRequestDetailResponse>> GetServiceRequestByIdAsync(CurrentUser user, int id);
    Task<Result> CreateServiceRequestAsync(CurrentUser user, ServiceRequestCreateRequest request);
    // A request is never edited or deleted: it is cancelled, so its history stays.
    Task<Result> CancelServiceRequestAsync(CurrentUser user, ServiceRequestCancelRequest request);
}
