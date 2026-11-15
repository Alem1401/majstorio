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
}
