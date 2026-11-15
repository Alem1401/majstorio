using Majstorio.Common.Services;
using Majstorio.Model.Requests.User;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.User;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Extensions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

public class UserService : IUserService
{
    private const string NotFoundMessage = "User not found.";
    private const string EmailTakenMessage = "A user with this email already exists.";
    private const string UserNameTakenMessage = "A user with this user name already exists.";

    private readonly MajstorioDbContext _ctx;
    private readonly IMapper _mapper;
    private readonly ICryptoService _cryptoService;

    public UserService(MajstorioDbContext ctx, IMapper mapper, ICryptoService cryptoService)
    {
        _ctx = ctx;
        _mapper = mapper;
        _cryptoService = cryptoService;
    }

    public async Task<Result<PagedResponse<UserResponse>>> GetUsersAsync(UserSearchRequest search)
    {
        var query = _ctx.ApplicationUsers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            var name = search.Name.Trim();
            query = query.Where(user => user.UserName.Contains(name)
                || user.FirstName.Contains(name) || user.LastName.Contains(name));
        }

        if (search.IsActive.HasValue)
            query = query.Where(user => user.IsActive == search.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(search.Role))
        {
            var role = Enum.Parse<UserRole>(search.Role, ignoreCase: true);
            query = query.Where(user => user.Role == role);
        }

        var page = await query
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.Id)
            .ToPagedAsync(search, users => _mapper.Map<List<UserResponse>>(users));

        return Result<PagedResponse<UserResponse>>.Success(page);
    }

    public async Task<Result<UserResponse>> GetUserByIdAsync(int id)
    {
        var user = await _ctx.ApplicationUsers.AsNoTracking().FirstOrDefaultAsync(user => user.Id == id);
        if (user == null)
            return Result<UserResponse>.Failure(ErrorType.NotFound, NotFoundMessage);

        return Result<UserResponse>.Success(_mapper.Map<UserResponse>(user));
    }

    public async Task<Result> CreateUserAsync(UserCreateRequest request)
    {
        if (await _ctx.ApplicationUsers.AnyAsync(user => user.Email == request.Email))
            return Result.Failure(ErrorType.Conflict, EmailTakenMessage);

        if (await _ctx.ApplicationUsers.AnyAsync(user => user.UserName == request.UserName))
            return Result.Failure(ErrorType.Conflict, UserNameTakenMessage);

        var user = _mapper.Map<ApplicationUser>(request);
        user.PasswordSalt = _cryptoService.GenerateSalt();
        user.PasswordHash = _cryptoService.GenerateHash(request.Password, user.PasswordSalt);
        _ctx.ApplicationUsers.Add(user);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateUserAsync(UserUpdateRequest request)
    {
        var user = await _ctx.ApplicationUsers.FirstOrDefaultAsync(user => user.Id == request.Id);
        if (user == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        if (await _ctx.ApplicationUsers.AnyAsync(other => other.Email == request.Email && other.Id != request.Id))
            return Result.Failure(ErrorType.Conflict, EmailTakenMessage);

        if (await _ctx.ApplicationUsers.AnyAsync(other => other.UserName == request.UserName && other.Id != request.Id))
            return Result.Failure(ErrorType.Conflict, UserNameTakenMessage);

        if (request.Role != null)
        {
            var role = Enum.Parse<UserRole>(request.Role, ignoreCase: true);
            // A tradesperson needs a profile with a city and services, so that role is never set from this form.
            if (role != user.Role && (role == UserRole.Tradesperson || user.Role == UserRole.Tradesperson))
                return Result.Failure(ErrorType.Conflict,
                    "The tradesperson role is granted or removed through the tradesperson profile, not by editing the user.");

            user.Role = role;
        }

        _mapper.Map(request, user);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteUserAsync(UserDeleteRequest request)
    {
        var user = await _ctx.ApplicationUsers.FirstOrDefaultAsync(user => user.Id == request.Id);
        if (user == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        // Users that other records still point to are deactivated instead, so the history stays intact.
        var isUsed = await _ctx.TradespersonProfiles.AnyAsync(profile => profile.UserId == user.Id || profile.ApprovedByUserId == user.Id)
            || await _ctx.ServiceRequests.AnyAsync(serviceRequest => serviceRequest.CustomerId == user.Id)
            || await _ctx.Offers.AnyAsync(offer => offer.DecidedByUserId == user.Id)
            || await _ctx.Reviews.AnyAsync(review => review.CustomerId == user.Id)
            || await _ctx.NewsPosts.AnyAsync(post => post.AuthorId == user.Id)
            || await _ctx.Notifications.AnyAsync(notification => notification.UserId == user.Id)
            || await _ctx.SearchLogs.AnyAsync(log => log.UserId == user.Id)
            || await _ctx.StatusHistories.AnyAsync(history => history.ChangedByUserId == user.Id)
            || await _ctx.Refunds.AnyAsync(refund => refund.RequestedByUserId == user.Id);
        if (isUsed)
            return Result.Failure(ErrorType.Conflict,
                "This user has requests, offers, reviews or other records, so the account cannot be deleted. Deactivate it instead.");

        _ctx.ApplicationUsers.Remove(user);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }
}
