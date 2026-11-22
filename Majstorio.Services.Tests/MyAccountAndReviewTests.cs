using Majstorio.Common.Services;
using Majstorio.Model.Requests.User;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Mapping;
using Majstorio.Services.Services;
using Majstorio.Services.Validators.User;
using Majstorio.WebAPI.Services;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

// The mobile app's own-account and rating actions: who may do what, and what is stored.
public class MyAccountAndReviewTests : IDisposable
{
    private readonly MajstorioDbContext _ctx = TestSupport.NewInMemoryContext();
    private readonly ReviewService _reviews;
    private readonly UserService _users;

    public MyAccountAndReviewTests()
    {
        var config = new TypeAdapterConfig();
        new ReviewMappingProfile().Register(config);
        new UserMappingProfile().Register(config);
        var mapper = new Mapper(config);
        _reviews = new ReviewService(_ctx, mapper);
        _users = new UserService(_ctx, mapper, new CryptoService());
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task Customer_RatesOwnCompletedJobOnce_AndSeesOnlyOwnReviews()
    {
        var (customer, appointment) = await AddJobAsync(AppointmentStatus.Completed);
        var (otherCustomer, otherAppointment) = await AddJobAsync(AppointmentStatus.Completed);
        var me = new CurrentUser(customer.Id, UserRole.Customer);

        Assert.True((await _reviews.CreateMyReviewAsync(me, new() { AppointmentId = appointment.Id, Rating = 4, Comment = "Uredno." })).IsSuccess);
        Assert.Equal(ErrorType.Conflict,
            (await _reviews.CreateMyReviewAsync(me, new() { AppointmentId = appointment.Id, Rating = 5 })).ErrorType);
        await _reviews.CreateMyReviewAsync(new CurrentUser(otherCustomer.Id, UserRole.Customer),
            new() { AppointmentId = otherAppointment.Id, Rating = 1 });

        var mine = (await _reviews.GetMyReviewsAsync(me, new())).Value!;
        var review = Assert.Single(mine.Items);
        Assert.Equal(4, review.Rating);
        Assert.Equal("Uredno.", review.Comment);
        Assert.Equal(customer.Id, review.CustomerId);

        var byAppointment = (await _reviews.GetMyReviewsAsync(me, new() { AppointmentId = otherAppointment.Id })).Value!;
        Assert.Empty(byAppointment.Items);
    }

    [Fact]
    public async Task Customer_CannotRateAnotherCustomersJob()
    {
        var (customer, _) = await AddJobAsync(AppointmentStatus.Completed);
        var (_, otherAppointment) = await AddJobAsync(AppointmentStatus.Completed);

        var result = await _reviews.CreateMyReviewAsync(new CurrentUser(customer.Id, UserRole.Customer),
            new() { AppointmentId = otherAppointment.Id, Rating = 5 });

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Empty(await _ctx.Reviews.ToListAsync());
    }

    [Fact]
    public async Task Customer_CannotRateAJobThatIsNotCompleted()
    {
        var (customer, appointment) = await AddJobAsync(AppointmentStatus.Confirmed);

        var result = await _reviews.CreateMyReviewAsync(new CurrentUser(customer.Id, UserRole.Customer),
            new() { AppointmentId = appointment.Id, Rating = 5 });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task UpdateMyProfile_ChangesOnlyPersonalDetails()
    {
        var user = TestSupport.NewUser();
        _ctx.ApplicationUsers.Add(user);
        await _ctx.SaveChangesAsync();
        var userName = user.UserName;

        var result = await _users.UpdateMyProfileAsync(user.Id, new()
        {
            Email = " nova@majstorio.test ", PhoneNumber = " ", FirstName = "Nova", LastName = "Osoba"
        });

        Assert.True(result.IsSuccess);
        _ctx.ChangeTracker.Clear();
        var stored = await _ctx.ApplicationUsers.SingleAsync(u => u.Id == user.Id);
        Assert.Equal("nova@majstorio.test", stored.Email);
        Assert.Null(stored.PhoneNumber);
        Assert.Equal("Nova", stored.FirstName);
        Assert.Equal(userName, stored.UserName);
        Assert.Equal(UserRole.Customer, stored.Role);
    }

    [Fact]
    public async Task UpdateMyProfile_RejectsAnEmailUsedByAnotherUser()
    {
        var user = TestSupport.NewUser();
        var other = TestSupport.NewUser();
        _ctx.ApplicationUsers.AddRange(user, other);
        await _ctx.SaveChangesAsync();

        var result = await _users.UpdateMyProfileAsync(user.Id, new()
        {
            Email = other.Email, FirstName = "A", LastName = "B"
        });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task SetProfileImage_ReturnsThePreviousAddress()
    {
        var user = TestSupport.NewUser();
        user.ProfileImageUrl = "Account/photos/old.jpg";
        _ctx.ApplicationUsers.Add(user);
        await _ctx.SaveChangesAsync();

        var result = await _users.SetProfileImageAsync(user.Id, "Account/photos/new.jpg");

        Assert.Equal("Account/photos/old.jpg", result.Value);
        Assert.Equal("Account/photos/new.jpg", (await _ctx.ApplicationUsers.SingleAsync()).ProfileImageUrl);
    }

    [Theory]
    [InlineData("ime@primjer.ba", "+387 61 123 456", "Ime", "Prezime", true)]
    [InlineData("", null, "Ime", "Prezime", false)]
    [InlineData("nije-email", null, "Ime", "Prezime", false)]
    [InlineData("ime@primjer.ba", "abc", "Ime", "Prezime", false)]
    [InlineData("ime@primjer.ba", null, "", "Prezime", false)]
    public void MyProfileValidator_ChecksEmailPhoneAndNames(string email, string? phone, string first, string last, bool isValid)
    {
        var request = new MyProfileUpdateRequest { Email = email, PhoneNumber = phone, FirstName = first, LastName = last };
        Assert.Equal(isValid, new MyProfileUpdateRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public async Task ImageStore_AcceptsJpegAndPng_RejectsOtherFilesAndForeignNames()
    {
        var folder = Path.Combine(Path.GetTempPath(), "majstorio-test-" + Guid.NewGuid().ToString("N"));
        var store = new ProfileImageStore(folder);
        try
        {
            var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3 };
            var (fileName, error) = await store.SaveAsync(new MemoryStream(png), png.Length);
            Assert.Null(error);
            Assert.EndsWith(".png", fileName);
            Assert.Equal("image/png", store.Find(fileName!)!.Value.ContentType);

            var text = "<script>"u8.ToArray();
            Assert.Null((await store.SaveAsync(new MemoryStream(text), text.Length)).FileName);
            Assert.Null((await store.SaveAsync(new MemoryStream(png), ProfileImageStore.MaxBytes + 1)).FileName);

            Assert.Null(store.Find("../appsettings.json"));
            Assert.Null(store.Find("photo.exe"));

            store.Delete(fileName);
            Assert.Null(store.Find(fileName!));
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    // A finished or running job: customer -> request -> offer -> appointment.
    private async Task<(ApplicationUser Customer, Appointment Appointment)> AddJobAsync(AppointmentStatus status)
    {
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
        _ctx.Appointments.Add(appointment);
        await _ctx.SaveChangesAsync();
        return (customer, appointment);
    }
}
