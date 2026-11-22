using Majstorio.Model.Requests.User;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.User;
using Majstorio.Model.Results;

public interface IUserService
{
    Task<Result<PagedResponse<UserResponse>>> GetUsersAsync(UserSearchRequest search);
    Task<Result<UserResponse>> GetUserByIdAsync(int id);
    Task<Result> CreateUserAsync(UserCreateRequest request);
    Task<Result> UpdateUserAsync(UserUpdateRequest request);
    Task<Result> DeleteUserAsync(UserDeleteRequest request);

    // The signed-in user's own account. The id comes from the token.
    Task<Result> UpdateMyProfileAsync(int userId, MyProfileUpdateRequest request);
    // Sets (or clears, with null) the user's photo address. Returns the previous address, so its file can be removed.
    Task<Result<string?>> SetProfileImageAsync(int userId, string? profileImageUrl);
}
