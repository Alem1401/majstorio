using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentValidation;
using Majstorio.Common.Services;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Mapping;
using Majstorio.Services.Services;
using Majstorio.Services.Validators.City;
using Majstorio.WebAPI.Controllers;
using Majstorio.WebAPI.Filters;
using Majstorio.WebAPI.Services;
using Majstorio.WebAPI.Services.Auth;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Majstorio.Services.Tests;

// Real HTTP requests through the real AccountController and MyReviewsController: JWT authentication,
// the validation filter, the result mapper, an in-memory database and a ProfileImageStore in a temp folder.
public class AccountAndReviewHttpTests : IAsyncLifetime
{
    private const string SigningKey = "test-signing-key-that-is-long-enough-for-hs256";
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegHeader = [0xFF, 0xD8, 0xFF, 0xE0];

    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "majstorio-http-" + Guid.NewGuid().ToString("N"));
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "Majstorio",
            ["Jwt:Audience"] = "Majstorio.Clients",
            ["Jwt:Key"] = SigningKey,
            ["Jwt:ExpiryMinutes"] = "60"
        });

        var mappingConfig = new TypeAdapterConfig();
        new ReviewMappingProfile().Register(mappingConfig);
        new UserMappingProfile().Register(mappingConfig);
        builder.Services.AddSingleton(mappingConfig);
        builder.Services.AddScoped<IMapper, ServiceMapper>();
        builder.Services.AddValidatorsFromAssemblyContaining<CityCreateRequestValidator>();
        builder.Services.AddControllers(options =>
            {
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                options.Filters.Add<RequestValidationFilter>();
            })
            .AddApplicationPart(typeof(AccountController).Assembly);
        builder.Services.AddSingleton<ResultToStatusCodeMapper>();
        builder.Services.AddSingleton(new ProfileImageStore(_folder));
        builder.Services.AddDbContext<MajstorioDbContext>(options => options
            .UseInMemoryDatabase(_databaseName)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        builder.Services.AddSingleton<ICryptoService, CryptoService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IReviewService, ReviewService>();
        builder.Services.AddJwtAuthentication(builder.Configuration);

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapControllers();
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
    }

    // ---------- authentication and roles ----------

    [Theory]
    [InlineData("GET", "/Account")]
    [InlineData("PUT", "/Account")]
    [InlineData("POST", "/Account/photo")]
    [InlineData("DELETE", "/Account/photo")]
    [InlineData("GET", "/Account/photos/0123456789abcdef0123456789abcdef.png")]
    [InlineData("GET", "/Reviews/mine")]
    [InlineData("POST", "/Reviews/mine")]
    public async Task Anonymous_IsUnauthorizedOnEveryRoute(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        // The photo upload only matches a multipart body; any other body gets 415 before authentication runs.
        if (path == "/Account/photo" && method == "POST")
            request.Content = new MultipartFormDataContent { { new ByteArrayContent(Png(64)), "file", "a.png" } };
        else if (method is "PUT" or "POST")
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(request)).StatusCode);
    }

    [Theory]
    [InlineData(UserRole.Tradesperson, "GET")]
    [InlineData(UserRole.Tradesperson, "POST")]
    [InlineData(UserRole.Administrator, "GET")]
    [InlineData(UserRole.Administrator, "POST")]
    public async Task MyReviews_AreForbiddenForNonCustomers(UserRole role, string method)
    {
        var user = await AddUserAsync(role);
        var request = Authorized(new HttpMethod(method), "/Reviews/mine", user);
        if (method == "POST")
            request.Content = JsonContent.Create(new { appointmentId = 1, rating = 5 });

        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(request)).StatusCode);
    }

    [Theory]
    [InlineData(UserRole.Customer)]
    [InlineData(UserRole.Tradesperson)]
    [InlineData(UserRole.Administrator)]
    public async Task Account_IsAvailableToEveryRole(UserRole role)
    {
        var user = await AddUserAsync(role);

        var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/Account", user));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        Assert.Equal(role.ToString(), body.RootElement.GetProperty("role").GetString());
    }

    [Fact]
    public async Task GetAccount_ReturnsOnlyTheCallersOwnData_WithoutSecrets()
    {
        var ana = await AddUserAsync(firstName: "Ana");
        var bob = await AddUserAsync(firstName: "Bob");

        using var anaBody = await ReadJsonAsync(await _client.SendAsync(Authorized(HttpMethod.Get, "/Account", ana)));
        using var bobBody = await ReadJsonAsync(await _client.SendAsync(Authorized(HttpMethod.Get, "/Account", bob)));

        Assert.Equal(ana.Id, anaBody.RootElement.GetProperty("id").GetInt32());
        Assert.Equal("Ana", anaBody.RootElement.GetProperty("firstName").GetString());
        Assert.Equal(bob.Id, bobBody.RootElement.GetProperty("id").GetInt32());
        Assert.Equal("Bob", bobBody.RootElement.GetProperty("firstName").GetString());
        var raw = anaBody.RootElement.GetRawText();
        Assert.DoesNotContain("password", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tokenVersion", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TokenIssuedBeforeLogout_CannotReadTheAccount()
    {
        var user = await AddUserAsync();
        var token = Token(user);

        await using (var scope = _app.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<IAuthService>().LogoutAsync(user.Id);

        var request = new HttpRequestMessage(HttpMethod.Get, "/Account");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(request)).StatusCode);
    }

    // ---------- PUT /Account ----------

    [Fact]
    public async Task PutAccount_InvalidBody_Returns400WithFieldErrors_AndChangesNothing()
    {
        var user = await AddUserAsync(firstName: "Ana");
        var request = Authorized(HttpMethod.Put, "/Account", user);
        request.Content = JsonContent.Create(new { email = "nije-email", phoneNumber = "(061) 123", firstName = "", lastName = "X" });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        var errors = body.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Email", out _), errors.GetRawText());
        Assert.True(errors.TryGetProperty("PhoneNumber", out _), errors.GetRawText());
        Assert.True(errors.TryGetProperty("FirstName", out _), errors.GetRawText());
        Assert.False(errors.TryGetProperty("LastName", out _), errors.GetRawText());
        Assert.Equal("Ana", (await FindUserAsync(user.Id)).FirstName);
    }

    [Fact]
    public async Task PutAccount_EmptyBody_Returns400()
    {
        var user = await AddUserAsync();
        var request = Authorized(HttpMethod.Put, "/Account", user);
        request.Content = new StringContent("", Encoding.UTF8, "application/json");

        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task PutAccount_IgnoresUserNameRoleAndPhotoInTheBody()
    {
        var user = await AddUserAsync();
        var request = Authorized(HttpMethod.Put, "/Account", user);
        request.Content = JsonContent.Create(new
        {
            email = "nova@majstorio.test", firstName = " Nova ", lastName = "Osoba", phoneNumber = "",
            userName = "hacker", role = "Administrator", isActive = false, profileImageUrl = "https://evil.test/x.png", id = 999
        });

        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(request)).StatusCode);

        var stored = await FindUserAsync(user.Id);
        Assert.Equal(user.UserName, stored.UserName);
        Assert.Equal(UserRole.Customer, stored.Role);
        Assert.True(stored.IsActive);
        Assert.Null(stored.ProfileImageUrl);
        Assert.Equal("Nova", stored.FirstName);
        Assert.Null(stored.PhoneNumber);
    }

    [Fact]
    public async Task PutAccount_EmailOfAnotherUserInOtherCase_Returns409WithMessage()
    {
        await AddUserAsync(email: "Foo@x.test");
        var user = await AddUserAsync();
        var request = Authorized(HttpMethod.Put, "/Account", user);
        request.Content = JsonContent.Create(new { email = "foo@X.TEST", firstName = "A", lastName = "B" });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("message").GetString()));
    }

    // ---------- photo upload ----------

    [Fact]
    public async Task UploadPhoto_WithoutAFile_Returns400WithMessage()
    {
        var user = await AddUserAsync();
        var request = Authorized(HttpMethod.Post, "/Account/photo", user);
        var form = new MultipartFormDataContent { { new StringContent("x"), "other" } };
        request.Content = form;

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Choose a photo to upload.", await MessageAsync(response));
    }

    [Fact]
    public async Task UploadPhoto_FileUnderAnotherFieldName_Returns400()
    {
        var user = await AddUserAsync();

        var response = await UploadAsync(user, Png(100), "slika.png", fieldName: "photo");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Choose a photo to upload.", await MessageAsync(response));
    }

    [Fact]
    public async Task UploadPhoto_TextFileNamedPng_Returns400_AndStoresNothing()
    {
        var user = await AddUserAsync();

        var response = await UploadAsync(user, "<html><script>alert(1)</script></html>"u8.ToArray(), "slika.png", "image/png");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Only JPEG and PNG photos are allowed.", await MessageAsync(response));
        Assert.Null((await FindUserAsync(user.Id)).ProfileImageUrl);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task UploadPhoto_EmptyFile_Returns400()
    {
        var user = await AddUserAsync();

        var response = await UploadAsync(user, [], "prazno.png");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task UploadPhoto_GifHeader_Returns400()
    {
        var user = await AddUserAsync();

        var response = await UploadAsync(user, "GIF89a\u0001\u0000"u8.ToArray(), "slika.gif", "image/gif");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadPhoto_JustOver2MB_Returns400WithSizeMessage()
    {
        var user = await AddUserAsync();

        var response = await UploadAsync(user, Png((int)ProfileImageStore.MaxBytes + 1), "velika.png");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("The photo can be at most 2 MB.", await MessageAsync(response));
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task UploadPhoto_Exactly2MB_IsAccepted()
    {
        var user = await AddUserAsync();

        var response = await UploadAsync(user, Png((int)ProfileImageStore.MaxBytes), "tacno.png");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(StoredFiles());
    }

    [Theory]
    [InlineData("png")]
    [InlineData("jpeg")]
    public async Task UploadPhoto_Valid_IsServedBackWithTheRightContentType(string kind)
    {
        var user = await AddUserAsync();
        var bytes = kind == "png" ? Png(500) : Jpeg(500);

        // The name the client sends does not decide the type: a JPEG called .png is still a JPEG.
        var upload = await UploadAsync(user, bytes, "upload.png");
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        var url = (await FindUserAsync(user.Id)).ProfileImageUrl!;
        Assert.Matches("^Account/photos/[0-9a-f]{32}\\.(jpg|png)$", url);
        var photo = await _client.SendAsync(Authorized(HttpMethod.Get, "/" + url, user));
        Assert.Equal(HttpStatusCode.OK, photo.StatusCode);
        Assert.Equal(kind == "png" ? "image/png" : "image/jpeg", photo.Content.Headers.ContentType!.MediaType);
        Assert.Equal(bytes, await photo.Content.ReadAsByteArrayAsync());

        using var account = await ReadJsonAsync(await _client.SendAsync(Authorized(HttpMethod.Get, "/Account", user)));
        Assert.Equal(url, account.RootElement.GetProperty("profileImageUrl").GetString());
    }

    [Fact]
    public async Task Photo_CanBeSeenByAnotherSignedInUser_ButNotAnonymously()
    {
        var owner = await AddUserAsync();
        var other = await AddUserAsync(UserRole.Tradesperson);
        await UploadAsync(owner, Png(64), "a.png");
        var url = "/" + (await FindUserAsync(owner.Id)).ProfileImageUrl;

        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(Authorized(HttpMethod.Get, url, other))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task ReplacingThePhoto_DeletesTheOldFile()
    {
        var user = await AddUserAsync();
        await UploadAsync(user, Png(64), "a.png");
        var first = (await FindUserAsync(user.Id)).ProfileImageUrl!;
        var firstFile = Path.Combine(_folder, first["Account/photos/".Length..]);
        Assert.True(File.Exists(firstFile));

        Assert.Equal(HttpStatusCode.OK, (await UploadAsync(user, Jpeg(64), "b.jpg")).StatusCode);

        var second = (await FindUserAsync(user.Id)).ProfileImageUrl!;
        Assert.NotEqual(first, second);
        Assert.False(File.Exists(firstFile));
        Assert.Single(StoredFiles());
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(Authorized(HttpMethod.Get, "/" + first, user))).StatusCode);
    }

    [Fact]
    public async Task ReplacingAnExternalPhoto_LeavesOtherFilesAlone()
    {
        // The seed points to an outside address whose last part looks like a stored name; that file belongs to nobody.
        const string lookalike = "0123456789abcdef0123456789abcdef.jpg";
        Directory.CreateDirectory(_folder);
        await File.WriteAllBytesAsync(Path.Combine(_folder, lookalike), Jpeg(10));
        var user = await AddUserAsync(profileImageUrl: "https://cdn.example.test/Account/photos/" + lookalike);

        Assert.Equal(HttpStatusCode.OK, (await UploadAsync(user, Png(64), "a.png")).StatusCode);

        Assert.True(File.Exists(Path.Combine(_folder, lookalike)));
        Assert.StartsWith("Account/photos/", (await FindUserAsync(user.Id)).ProfileImageUrl);
    }

    [Fact]
    public async Task DeletingAnExternalPhoto_ClearsTheAddressOnly()
    {
        const string lookalike = "fedcba9876543210fedcba9876543210.png";
        Directory.CreateDirectory(_folder);
        await File.WriteAllBytesAsync(Path.Combine(_folder, lookalike), Png(10));
        var user = await AddUserAsync(profileImageUrl: "https://example.test/" + lookalike);

        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(Authorized(HttpMethod.Delete, "/Account/photo", user))).StatusCode);

        Assert.Null((await FindUserAsync(user.Id)).ProfileImageUrl);
        Assert.True(File.Exists(Path.Combine(_folder, lookalike)));
    }

    [Fact]
    public async Task DeletePhoto_RemovesTheFileAndClearsTheAddress()
    {
        var user = await AddUserAsync();
        await UploadAsync(user, Png(64), "a.png");
        var url = (await FindUserAsync(user.Id)).ProfileImageUrl!;

        var response = await _client.SendAsync(Authorized(HttpMethod.Delete, "/Account/photo", user));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null((await FindUserAsync(user.Id)).ProfileImageUrl);
        Assert.Empty(StoredFiles());
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(Authorized(HttpMethod.Get, "/" + url, user))).StatusCode);
    }

    [Fact]
    public async Task DeletePhoto_WhenThereIsNone_IsOk()
    {
        var user = await AddUserAsync();

        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(Authorized(HttpMethod.Delete, "/Account/photo", user))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(Authorized(HttpMethod.Delete, "/Account/photo", user))).StatusCode);
    }

    [Fact]
    public async Task DeletePhoto_OfOneUser_DoesNotTouchAnotherUsersPhoto()
    {
        var ana = await AddUserAsync();
        var bob = await AddUserAsync();
        await UploadAsync(ana, Png(64), "a.png");
        await UploadAsync(bob, Png(64), "b.png");
        var bobUrl = (await FindUserAsync(bob.Id)).ProfileImageUrl!;

        await _client.SendAsync(Authorized(HttpMethod.Delete, "/Account/photo", ana));

        Assert.Equal(bobUrl, (await FindUserAsync(bob.Id)).ProfileImageUrl);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(Authorized(HttpMethod.Get, "/" + bobUrl, ana))).StatusCode);
    }

    [Theory]
    [InlineData("..%2f..%2fappsettings.json")]
    [InlineData("..%2F..%2FMajstorio.WebAPI.dll")]
    [InlineData("%2e%2e%5cappsettings.json")]
    [InlineData("x.jpg")]
    [InlineData("0123456789ABCDEF0123456789ABCDEF.png")]
    [InlineData("0123456789abcdef0123456789abcdef.gif")]
    [InlineData("0123456789abcdef0123456789abcdef.png.exe")]
    [InlineData("0123456789abcdef0123456789abcdef.png")]
    public async Task GetPhoto_OddOrForeignNames_Return404(string fileName)
    {
        // A file that exists next to the store must never be reachable through a crafted name.
        Directory.CreateDirectory(_folder);
        await File.WriteAllTextAsync(Path.Combine(_folder, "x.jpg"), "not a photo");
        await File.WriteAllTextAsync(Path.Combine(_folder, "0123456789abcdef0123456789abcdef.gif"), "gif");
        var user = await AddUserAsync();

        var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/Account/photos/" + fileName, user));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Reviews/mine ----------

    [Fact]
    public async Task MyReviews_CreateThenFindByAppointment_AndSecondAttemptIs409()
    {
        var (customer, appointment) = await AddJobAsync(AppointmentStatus.Completed);
        var post = Authorized(HttpMethod.Post, "/Reviews/mine", customer);
        post.Content = JsonContent.Create(new { appointmentId = appointment.Id, rating = 5, comment = "  Odlično  " });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(post)).StatusCode);

        var again = Authorized(HttpMethod.Post, "/Reviews/mine", customer);
        again.Content = JsonContent.Create(new { appointmentId = appointment.Id, rating = 1 });
        var conflict = await _client.SendAsync(again);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await MessageAsync(conflict)));

        using var list = await ReadJsonAsync(await _client.SendAsync(
            Authorized(HttpMethod.Get, $"/Reviews/mine?appointmentId={appointment.Id}&pageSize=1", customer)));
        var item = Assert.Single(list.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(5, item.GetProperty("rating").GetInt32());
        Assert.Equal("Odlično", item.GetProperty("comment").GetString());
        Assert.Equal(1, list.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task MyReviews_AnotherCustomersAppointment_Is404()
    {
        var (customer, _) = await AddJobAsync(AppointmentStatus.Completed);
        var (_, foreign) = await AddJobAsync(AppointmentStatus.Completed);
        var post = Authorized(HttpMethod.Post, "/Reviews/mine", customer);
        post.Content = JsonContent.Create(new { appointmentId = foreign.Id, rating = 5 });

        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(post)).StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public async Task MyReviews_RatingOutOfRange_Is400WithRatingError(int rating)
    {
        var (customer, appointment) = await AddJobAsync(AppointmentStatus.Completed);
        var post = Authorized(HttpMethod.Post, "/Reviews/mine", customer);
        post.Content = JsonContent.Create(new { appointmentId = appointment.Id, rating });

        var response = await _client.SendAsync(post);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("Rating", out _));
    }

    [Fact]
    public async Task MyReviews_CommentOver2000_Is400()
    {
        var (customer, appointment) = await AddJobAsync(AppointmentStatus.Completed);
        var post = Authorized(HttpMethod.Post, "/Reviews/mine", customer);
        post.Content = JsonContent.Create(new { appointmentId = appointment.Id, rating = 3, comment = new string('a', 2001) });

        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(post)).StatusCode);
    }

    [Theory]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?page=0")]
    [InlineData("?rating=6")]
    [InlineData("?appointmentId=abc")]
    public async Task MyReviews_InvalidQuery_Is400(string query)
    {
        var customer = await AddUserAsync();

        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.SendAsync(Authorized(HttpMethod.Get, "/Reviews/mine" + query, customer))).StatusCode);
    }

    // ---------- helpers ----------

    private static byte[] Png(int length)
    {
        var bytes = new byte[length];
        PngHeader.CopyTo(bytes, 0);
        for (var i = PngHeader.Length; i < length; i++) bytes[i] = (byte)(i % 251);
        return bytes;
    }

    private static byte[] Jpeg(int length)
    {
        var bytes = new byte[length];
        JpegHeader.CopyTo(bytes, 0);
        for (var i = JpegHeader.Length; i < length; i++) bytes[i] = (byte)(i % 253);
        return bytes;
    }

    private string[] StoredFiles() => Directory.Exists(_folder) ? Directory.GetFiles(_folder) : [];

    private Task<HttpResponseMessage> UploadAsync(ApplicationUser user, byte[] bytes, string fileName,
        string contentType = "application/octet-stream", string fieldName = "file")
    {
        var request = Authorized(HttpMethod.Post, "/Account/photo", user);
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Content = new MultipartFormDataContent { { file, fieldName, fileName } };
        return _client.SendAsync(request);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private static async Task<string?> MessageAsync(HttpResponseMessage response)
    {
        using var body = await ReadJsonAsync(response);
        return body.RootElement.GetProperty("message").GetString();
    }

    private string Token(ApplicationUser user) =>
        _app.Services.GetRequiredService<ITokenService>().CreateAccessToken(user).Token;

    private HttpRequestMessage Authorized(HttpMethod method, string path, ApplicationUser user)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token(user));
        return request;
    }

    private async Task<ApplicationUser> AddUserAsync(UserRole role = UserRole.Customer, string? firstName = null,
        string? email = null, string? profileImageUrl = null)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MajstorioDbContext>();
        var user = TestSupport.NewUser(role);
        if (firstName != null) user.FirstName = firstName;
        if (email != null) user.Email = email;
        user.ProfileImageUrl = profileImageUrl;
        ctx.ApplicationUsers.Add(user);
        await ctx.SaveChangesAsync();
        return user;
    }

    private async Task<ApplicationUser> FindUserAsync(int id)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<MajstorioDbContext>()
            .ApplicationUsers.AsNoTracking().SingleAsync(user => user.Id == id);
    }

    private async Task<(ApplicationUser Customer, Appointment Appointment)> AddJobAsync(AppointmentStatus status)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MajstorioDbContext>();
        var customer = TestSupport.NewUser();
        var city = new City { Name = Guid.NewGuid().ToString() };
        var appointment = new Appointment
        {
            Status = status,
            Offer = new Offer
            {
                TradespersonProfile = new TradespersonProfile { User = TestSupport.NewUser(UserRole.Tradesperson), City = city },
                ServiceRequest = new ServiceRequest
                {
                    Customer = customer, City = city,
                    ServiceCategory = new ServiceCategory { Name = Guid.NewGuid().ToString() }
                }
            }
        };
        ctx.Appointments.Add(appointment);
        await ctx.SaveChangesAsync();
        return (customer, appointment);
    }
}
