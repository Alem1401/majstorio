using Majstorio.Common.Services;
using Majstorio.Model.Requests.User;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Mapping;
using Majstorio.Services.Services;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

public class UserServiceTests : IDisposable
{
    private readonly MajstorioDbContext _ctx;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _ctx = new MajstorioDbContext(new DbContextOptionsBuilder<MajstorioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = new TypeAdapterConfig();
        new UserMappingProfile().Register(config);
        _service = new UserService(_ctx, new Mapper(config), new CryptoService());
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task Get_FiltersByNameRoleAndActiveAndReturnsRoleName()
    {
        await AddUserAsync("admin", UserRole.Administrator);
        await AddUserAsync("amra", UserRole.Customer);
        await AddUserAsync("lejla", UserRole.Customer, isActive: false);

        var customers = (await _service.GetUsersAsync(new() { Role = "Customer" })).Value!;
        Assert.Equal(2, customers.TotalCount);
        Assert.All(customers.Items, user => Assert.Equal("Customer", user.Role));

        var active = (await _service.GetUsersAsync(new() { Role = "customer", IsActive = true })).Value!;
        Assert.Equal("amra", Assert.Single(active.Items).UserName);

        var byName = (await _service.GetUsersAsync(new() { Name = "adm" })).Value!;
        Assert.Equal("Administrator", Assert.Single(byName.Items).Role);
    }

    [Fact]
    public async Task Update_KeepsPasswordAndRoleWhenRoleIsLeftOut()
    {
        var user = await AddUserAsync("admin", UserRole.Administrator);

        var result = await _service.UpdateUserAsync(Request(user, firstName: "Changed"));

        Assert.True(result.IsSuccess);
        _ctx.ChangeTracker.Clear();
        var stored = await _ctx.ApplicationUsers.SingleAsync();
        Assert.Equal("Changed", stored.FirstName);
        Assert.Equal(UserRole.Administrator, stored.Role);
        Assert.Equal("hash", stored.PasswordHash);
        Assert.Equal("salt", stored.PasswordSalt);
    }

    [Fact]
    public async Task Update_ChangesRoleBetweenCustomerAndAdministrator_AndInvalidatesOldTokens()
    {
        var user = await AddUserAsync("amra", UserRole.Customer);
        var tokenVersion = user.TokenVersion;

        Assert.True((await _service.UpdateUserAsync(Request(user, role: "Administrator"))).IsSuccess);

        _ctx.ChangeTracker.Clear();
        var stored = await _ctx.ApplicationUsers.SingleAsync();
        Assert.Equal(UserRole.Administrator, stored.Role);
        Assert.Equal(tokenVersion + 1, stored.TokenVersion);
    }

    [Fact]
    public async Task Update_SameRole_KeepsTokensValid()
    {
        var user = await AddUserAsync("amra", UserRole.Customer);
        var tokenVersion = user.TokenVersion;

        Assert.True((await _service.UpdateUserAsync(Request(user, role: "Customer"))).IsSuccess);

        _ctx.ChangeTracker.Clear();
        Assert.Equal(tokenVersion, (await _ctx.ApplicationUsers.SingleAsync()).TokenVersion);
    }

    [Theory]
    [InlineData(UserRole.Customer, "Tradesperson")]
    [InlineData(UserRole.Tradesperson, "Customer")]
    public async Task Update_TradespersonRoleCannotBeSetOrRemoved(UserRole current, string requested)
    {
        var user = await AddUserAsync("emir", current);

        var result = await _service.UpdateUserAsync(Request(user, role: requested));

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _ctx.ChangeTracker.Clear();
        Assert.Equal(current, (await _ctx.ApplicationUsers.SingleAsync()).Role);
    }

    [Fact]
    public async Task Update_EmailOrUserNameOfAnotherUser_ReturnsConflict()
    {
        var first = await AddUserAsync("first", UserRole.Customer);
        var second = await AddUserAsync("second", UserRole.Customer);

        var sameEmail = Request(second);
        sameEmail.Email = first.Email;
        Assert.Equal(ErrorType.Conflict, (await _service.UpdateUserAsync(sameEmail)).ErrorType);

        var sameUserName = Request(second);
        sameUserName.UserName = first.UserName;
        Assert.Equal(ErrorType.Conflict, (await _service.UpdateUserAsync(sameUserName)).ErrorType);

        // Saving a user with their own unchanged email and user name is allowed.
        Assert.True((await _service.UpdateUserAsync(Request(second))).IsSuccess);
    }

    [Fact]
    public async Task Delete_UserWithRecords_ReturnsConflictAndUnusedUserIsRemoved()
    {
        var author = await AddUserAsync("author", UserRole.Administrator);
        var unused = await AddUserAsync("unused", UserRole.Customer);
        _ctx.NewsPosts.Add(new NewsPost { AuthorId = author.Id, Title = "Title", Content = "Content", ImageUrl = "image" });
        await _ctx.SaveChangesAsync();

        Assert.Equal(ErrorType.Conflict, (await _service.DeleteUserAsync(new() { Id = author.Id })).ErrorType);
        Assert.True((await _service.DeleteUserAsync(new() { Id = unused.Id })).IsSuccess);
        Assert.Equal(ErrorType.NotFound, (await _service.DeleteUserAsync(new() { Id = unused.Id })).ErrorType);
        Assert.Equal("author", (await _ctx.ApplicationUsers.SingleAsync()).UserName);
    }

    private async Task<ApplicationUser> AddUserAsync(string userName, UserRole role, bool isActive = true)
    {
        var user = new ApplicationUser
        {
            UserName = userName, Email = $"{userName}@example.com", FirstName = userName, LastName = "Test",
            PasswordHash = "hash", PasswordSalt = "salt", Role = role, IsActive = isActive
        };
        _ctx.ApplicationUsers.Add(user);
        await _ctx.SaveChangesAsync();
        return user;
    }

    private static UserUpdateRequest Request(ApplicationUser user, string? firstName = null, string? role = null) => new()
    {
        Id = user.Id, UserName = user.UserName, Email = user.Email, FirstName = firstName ?? user.FirstName,
        LastName = user.LastName, IsActive = user.IsActive, Role = role
    };
}
