using Majstorio.Common.Services;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Majstorio.Services.Tests;

public class SeedDataTests : IDisposable
{
    private readonly MajstorioDbContext _ctx;

    public SeedDataTests()
    {
        _ctx = new MajstorioDbContext(new DbContextOptionsBuilder<MajstorioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _ctx.Database.EnsureCreated();
    }

    [Fact]
    public void EveryTable_HasSeedData()
    {
        // Seed data lives only in the design-time model; the runtime model drops it.
        foreach (var entityType in _ctx.GetService<IDesignTimeModel>().Model.GetEntityTypes())
        {
            var rowCount = entityType.GetSeedData().Count();
            Assert.True(rowCount > 0, $"{entityType.ClrType.Name} has no seed data.");
        }
    }

    [Theory]
    [InlineData("desktop", UserRole.Administrator)]
    [InlineData("mobile", UserRole.Customer)]
    [InlineData("tradesperson", UserRole.Tradesperson)]
    [InlineData("electrician", UserRole.Tradesperson)]
    [InlineData("customer", UserRole.Customer)]
    [InlineData("applicant", UserRole.Customer)]
    public async Task SeededAccounts_LogInWithPasswordTest(string userName, UserRole role)
    {
        var user = await _ctx.ApplicationUsers.SingleAsync(user => user.UserName == userName);

        Assert.Equal(role, user.Role);
        Assert.True(user.IsActive);
        Assert.True(new CryptoService().VerifyPassword("test", user.PasswordHash, user.PasswordSalt));
    }

    [Fact]
    public async Task OnlyApprovedProfiles_BelongToTradespeople()
    {
        var profiles = await _ctx.TradespersonProfiles.Include(profile => profile.User).ToListAsync();

        Assert.All(profiles, profile =>
            Assert.Equal(profile.IsApproved, profile.User.Role == UserRole.Tradesperson));
    }

    public void Dispose() => _ctx.Dispose();
}
