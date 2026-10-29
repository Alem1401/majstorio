using Majstorio.Model.Requests.User;
using Majstorio.Model.Results;
using Majstorio.Model.Responses.User;
using Majstorio.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Majstorio.Common.Services;

namespace Majstorio.Services.Services;

public class UserService : IUserService
{
    MajstorioDbContext _ctx;
    IMapper _mapper;
    ICryptoService _cryptoService;

    public UserService(MajstorioDbContext ctx, IMapper mapper,ICryptoService cryptoService)
    {
        _ctx = ctx;
        _mapper = mapper;
        _cryptoService = cryptoService;
    }

    public async Task<Result> CreateUserAsync(UserCreateRequest request)
    {
        var userExists = await _ctx.ApplicationUsers.AnyAsync(u => u.Email == request.Email);
        if (userExists) {
            return Result.Failure(ErrorType.Conflict, "User with that email already exists");
        }
        var toAdd = _mapper.Map<ApplicationUser>(request);
        var salt = _cryptoService.GenerateSlat();
        var hash = _cryptoService.GenerateHash(salt, request.Password);
        toAdd.PasswordSalt = salt;
        toAdd.PasswordHash = hash;
        await _ctx.ApplicationUsers.AddAsync(toAdd);
        await _ctx.SaveChangesAsync();
        return Result.Success();

    }

    public async Task<Result> UpdateUserAsync(UserUpdateRequest request)
    {
        var toUpdate =await _ctx.ApplicationUsers.FirstOrDefaultAsync(u => u.Id ==  request.Id);
        if (toUpdate == null) {
            return Result.Failure(ErrorType.NotFound,"User with that id was not found");
        }
        if(_ctx.ApplicationUsers.Any(u => u.Email == request.Email))
        {
            return Result.Failure(ErrorType.Conflict, "Email already taken");
        }
        toUpdate = _mapper.Map<ApplicationUser>(request);
        _ctx.Update(toUpdate);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteUserAsync(UserDeleteRequest request)
    {
        var toDelete = await _ctx.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == request.Id);
        if (toDelete == null)
        {
            return Result.Failure(ErrorType.NotFound, "User with that id was not found");
        }

        _ctx.ApplicationUsers.Remove(toDelete);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<UserResponse>>> GetAllUsersAsync()
    {
        var users = await _ctx.ApplicationUsers.ToListAsync();
        var response = _mapper.Map<List<UserResponse>>(users);
        return Result<List<UserResponse>>.Success(response);
    }

    public async Task<Result<UserResponse>> GetUserByIdAsync(int id)
    {
        var user = await _ctx.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return Result<UserResponse>.Failure(ErrorType.NotFound, "User with that id was not found");
        }

        var response = _mapper.Map<UserResponse>(user);
        return Result<UserResponse>.Success(response);
    }
}
