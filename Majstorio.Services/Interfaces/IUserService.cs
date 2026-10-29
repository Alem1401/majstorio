using Majstorio.Model.Requests.User;
using Majstorio.Model.Results;
using Majstorio.Model.Responses.User;

public interface IUserService
{
    Task<Result> CreateUserAsync(UserCreateRequest request);
    Task<Result> UpdateUserAsync(UserUpdateRequest request);
    Task<Result> DeleteUserAsync(UserDeleteRequest request);
    Task<Result<List<UserResponse>>> GetAllUsersAsync();
    Task<Result<UserResponse>> GetUserByIdAsync(int id);
}
