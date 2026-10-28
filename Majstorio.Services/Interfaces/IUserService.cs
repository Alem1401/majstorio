using Majstorio.Model.Requests.User;
using Majstorio.Model.Responses.User;

public interface IUserService
{
    Task<bool> CreateUserAsync(UserCreateRequest request);
    Task<bool> UpdateUserAsync(UserUpdateRequest request);
    Task<bool> DeleteUserAsync(UserDeleteRequest request);
    Task<List<UserResponse>> GetAllUsersAsync();
    Task<UserResponse?> GetUserByIdAsync(int id);
}
