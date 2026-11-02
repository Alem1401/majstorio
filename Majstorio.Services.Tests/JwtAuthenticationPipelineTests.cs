using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Majstorio.Common.Services;
using Majstorio.Services.Constants;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Services;
using Majstorio.WebAPI.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Majstorio.Services.Tests;

// Sends real HTTP requests through the same JWT registration Program.cs uses, against test-only endpoints.
public class JwtAuthenticationPipelineTests : IAsyncLifetime
{
    private const string SigningKey = "test-signing-key-that-is-long-enough-for-hs256";

    private readonly string _databaseName = Guid.NewGuid().ToString();
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "Majstorio",
            ["Jwt:Audience"] = "Majstorio.Clients",
            ["Jwt:Key"] = SigningKey,
            ["Jwt:ExpiryMinutes"] = "60"
        });
        builder.Services.AddDbContext<MajstorioDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        builder.Services.AddSingleton<ICryptoService, CryptoService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddJwtAuthentication(builder.Configuration);

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapGet("/me", (ClaimsPrincipal user) => user.Identity!.Name).RequireAuthorization();
        _app.MapGet("/admin", () => "admin").RequireAuthorization(new AuthorizeAttribute { Roles = RoleNames.Administrator });

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    [Fact]
    public async Task NoToken_IsUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/me")).StatusCode);
    }

    [Fact]
    public async Task ValidToken_AuthenticatesTheUserByName()
    {
        var user = await AddUserAsync("ana");

        var response = await GetAsync("/me", CreateToken(user));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ana", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RoleCheck_UsesTheRoleClaimFromTheToken()
    {
        var customer = await AddUserAsync("ana");
        var admin = await AddUserAsync("admin", UserRole.Administrator);

        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync("/admin", CreateToken(customer))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync("/admin", CreateToken(admin))).StatusCode);
    }

    [Fact]
    public async Task TokenIssuedBeforeLogout_IsRejected()
    {
        var user = await AddUserAsync("ana");
        var token = CreateToken(user);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync("/me", token)).StatusCode);

        await using (var scope = _app.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<IAuthService>().LogoutAsync(user.Id);

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync("/me", token)).StatusCode);
    }

    [Fact]
    public async Task TokenOfADeactivatedUser_IsRejected()
    {
        var user = await AddUserAsync("ana", isActive: false);

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync("/me", CreateToken(user))).StatusCode);
    }

    [Fact]
    public async Task TokenSignedWithAnotherKey_IsRejected()
    {
        var user = await AddUserAsync("ana");
        var forger = NewTokenService("a-different-signing-key-that-is-long-enough", TimeProvider.System);

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync("/me", forger.CreateAccessToken(user).Token)).StatusCode);
    }

    [Fact]
    public async Task ExpiredToken_IsRejected()
    {
        var user = await AddUserAsync("ana");
        var past = NewTokenService(SigningKey, new FixedTimeProvider(DateTimeOffset.UtcNow.AddHours(-2)));

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync("/me", past.CreateAccessToken(user).Token)).StatusCode);
    }

    private string CreateToken(ApplicationUser user) =>
        _app.Services.GetRequiredService<ITokenService>().CreateAccessToken(user).Token;

    private Task<HttpResponseMessage> GetAsync(string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request);
    }

    private async Task<ApplicationUser> AddUserAsync(string userName, UserRole role = UserRole.Customer, bool isActive = true)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MajstorioDbContext>();
        var user = new ApplicationUser
        {
            UserName = userName, Email = $"{userName}@majstorio.test",
            PasswordHash = "unused", PasswordSalt = "unused", Role = role, IsActive = isActive
        };
        ctx.ApplicationUsers.Add(user);
        await ctx.SaveChangesAsync();
        return user;
    }

    private static JwtTokenService NewTokenService(string key, TimeProvider timeProvider) => new(Options.Create(new JwtOptions
    {
        Issuer = "Majstorio", Audience = "Majstorio.Clients", Key = key, ExpiryMinutes = 60
    }), timeProvider);

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
