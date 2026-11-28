using Majstorio.Model.Requests.TradespersonProfile;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.TradespersonProfile;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;

public interface ITradespersonProfileService
{
    // Everyone except administrators gets only approved, active profiles.
    Task<Result<PagedResponse<TradespersonProfileResponse>>> GetProfilesAsync(CurrentUser user, TradespersonProfileSearchRequest search);
    Task<Result<TradespersonProfileDetailResponse>> GetProfileByIdAsync(CurrentUser user, int id);

    // Administrator actions. Approval also gives the profile's user the Tradesperson role.
    Task<Result> UpdateProfileAsync(TradespersonProfileUpdateRequest request);
    Task<Result> ApproveProfileAsync(CurrentUser administrator, int id);
    Task<Result> SetProfileActiveAsync(TradespersonProfileActiveRequest request);
}
