using Majstorio.Model.Requests.User;
using Majstorio.Model.Responses.User;

namespace Majstorio.Services.Services;

public class UserService : IUserService
{
    public Task<bool> CreateUserAsync(UserCreateRequest request) => throw new NotImplementedException();

    public Task<bool> UpdateUserAsync(UserUpdateRequest request) => throw new NotImplementedException();

    public Task<bool> DeleteUserAsync(UserDeleteRequest request) => throw new NotImplementedException();

    public Task<List<UserResponse>> GetAllUsersAsync() => throw new NotImplementedException();

    public Task<UserResponse?> GetUserByIdAsync(int id) => throw new NotImplementedException();
}
