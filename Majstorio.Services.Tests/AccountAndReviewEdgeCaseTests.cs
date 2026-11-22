using Majstorio.Common.Services;
using Majstorio.Model.Requests.Review;
using Majstorio.Model.Requests.User;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Mapping;
using Majstorio.Services.Services;
using Majstorio.Services.Validators.Review;
using Majstorio.Services.Validators.User;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

// Boundaries and odd inputs of the customer ratings and the own-profile service methods and validators.
public class AccountAndReviewEdgeCaseTests : IDisposable
{
    private readonly MajstorioDbContext _ctx = TestSupport.NewInMemoryContext();
    private readonly ReviewService _reviews;
    private readonly UserService _users;

    public AccountAndReviewEdgeCaseTests()
    {
        var config = new TypeAdapterConfig();
        new ReviewMappingProfile().Register(config);
        new UserMappingProfile().Register(config);
        var mapper = new Mapper(config);
        _reviews = new ReviewService(_ctx, mapper);
        _users = new UserService(_ctx, mapper, new CryptoService());
    }

    public void Dispose() => _ctx.Dispose();

    // ---------- review validator ----------

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(int.MaxValue, false)]
    public void ReviewValidator_RatingBoundaries(int rating, bool isValid)
    {
        var result = new ReviewCreateRequestValidator().Validate(new ReviewCreateRequest { AppointmentId = 1, Rating = rating });
        Assert.Equal(isValid, result.IsValid);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(0, true)]
    [InlineData(2000, true)]
    [InlineData(2001, false)]
    public void ReviewValidator_CommentLength(int? length, bool isValid)
    {
        var request = new ReviewCreateRequest
        {
            AppointmentId = 1, Rating = 5, Comment = length == null ? null : new string('ž', length.Value)
        };
        Assert.Equal(isValid, new ReviewCreateRequestValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ReviewValidator_RequiresAnAppointment(int appointmentId)
    {
        Assert.False(new ReviewCreateRequestValidator()
            .Validate(new ReviewCreateRequest { AppointmentId = appointmentId, Rating = 5 }).IsValid);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(6, false)]
    public void ReviewSearchValidator_RatingFilter(int? rating, bool isValid)
    {
        Assert.Equal(isValid, new ReviewSearchRequestValidator().Validate(new ReviewSearchRequest { Rating = rating }).IsValid);
    }

    // ---------- creating a rating ----------

    [Theory]
    [InlineData(" ")]
    [InlineData("\t\r\n  ")]
    [InlineData("")]
    public async Task WhitespaceComment_IsStoredAsNull(string comment)
    {
        var (customer, appointment) = await AddJobAsync(AppointmentStatus.Completed);

        Assert.True((await _reviews.CreateMyReviewAsync(TestSupport.As(customer),
            new() { AppointmentId = appointment.Id, Rating = 3, Comment = comment })).IsSuccess);

        Assert.Null((await _ctx.Reviews.SingleAsync()).Comment);
    }

    [Fact]
    public async Task Comment_IsTrimmed_ButInnerSpacingKept()
    {
        var (customer, appointment) = await AddJobAsync(AppointmentStatus.Completed);

        await _reviews.CreateMyReviewAsync(TestSupport.As(customer),
            new() { AppointmentId = appointment.Id, Rating = 4, Comment = "\n  Brzo  i   uredno.  \t" });

        Assert.Equal("Brzo  i   uredno.", (await _ctx.Reviews.SingleAsync()).Comment);
    }

    [Fact]
    public async Task Comment_OfExactly2000Characters_IsStoredWhole()
    {
        var (customer, appointment) = await AddJobAsync(AppointmentStatus.Completed);
        var comment = new string('x', 2000);

        Assert.True((await _reviews.CreateMyReviewAsync(TestSupport.As(customer),
            new() { AppointmentId = appointment.Id, Rating = 5, Comment = comment })).IsSuccess);

        Assert.Equal(comment, (await _ctx.Reviews.SingleAsync()).Comment);
    }

    [Theory]
    [InlineData(AppointmentStatus.Pending)]
    [InlineData(AppointmentStatus.Confirmed)]
    [InlineData(AppointmentStatus.Cancelled)]
    public async Task UnfinishedOrCancelledJob_IsConflict_AndNothingStored(AppointmentStatus status)
    {
        var (customer, appointment) = await AddJobAsync(status);

        var result = await _reviews.CreateMyReviewAsync(TestSupport.As(customer),
            new() { AppointmentId = appointment.Id, Rating = 5 });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Empty(await _ctx.Reviews.ToListAsync());
    }

    [Fact]
    public async Task MissingAppointment_IsNotFound()
    {
        var (customer, _) = await AddJobAsync(AppointmentStatus.Completed);

        var result = await _reviews.CreateMyReviewAsync(TestSupport.As(customer), new() { AppointmentId = 987654, Rating = 5 });

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    // The status of another customer's job must not leak: "not completed" would reveal it exists.
    [Theory]
    [InlineData(AppointmentStatus.Completed)]
    [InlineData(AppointmentStatus.Cancelled)]
    [InlineData(AppointmentStatus.Pending)]
    public async Task AnotherCustomersAppointment_IsNotFound_WhateverItsStatus(AppointmentStatus status)
    {
        var (customer, _) = await AddJobAsync(AppointmentStatus.Completed);
        var (_, foreign) = await AddJobAsync(status);

        var result = await _reviews.CreateMyReviewAsync(TestSupport.As(customer), new() { AppointmentId = foreign.Id, Rating = 5 });

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Empty(await _ctx.Reviews.ToListAsync());
    }

    [Fact]
    public async Task TheTradespersonOfTheJob_CannotRateIt()
    {
        var (_, appointment) = await AddJobAsync(AppointmentStatus.Completed);
        var tradesperson = await _ctx.TradespersonProfiles.Include(p => p.User).SingleAsync();

        var result = await _reviews.CreateMyReviewAsync(TestSupport.As(tradesperson.User),
            new() { AppointmentId = appointment.Id, Rating = 5 });

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task StoredReview_TakesCustomerAndTradespersonFromTheJob()
    {
        var (customer, appointment) = await AddJobAsync(AppointmentStatus.Completed);
        var profile = await _ctx.TradespersonProfiles.SingleAsync();

        await _reviews.CreateMyReviewAsync(TestSupport.As(customer), new() { AppointmentId = appointment.Id, Rating = 2 });

        var review = await _ctx.Reviews.SingleAsync();
        Assert.Equal(customer.Id, review.CustomerId);
        Assert.Equal(profile.Id, review.TradespersonProfileId);
        Assert.Equal(appointment.Id, review.AppointmentId);
        Assert.Equal(2, review.Rating);
    }

    // ---------- listing own ratings ----------

    [Fact]
    public async Task GetMyReviews_PagesOnlyOwnReviews()
    {
        var customer = TestSupport.NewUser();
        _ctx.ApplicationUsers.Add(customer);
        await _ctx.SaveChangesAsync();
        for (var i = 1; i <= 3; i++)
        {
            var appointment = await AddJobAsync(AppointmentStatus.Completed, customer);
            await _reviews.CreateMyReviewAsync(TestSupport.As(customer), new() { AppointmentId = appointment.Appointment.Id, Rating = i });
        }
        var (other, otherAppointment) = await AddJobAsync(AppointmentStatus.Completed);
        await _reviews.CreateMyReviewAsync(TestSupport.As(other), new() { AppointmentId = otherAppointment.Id, Rating = 5 });

        var first = (await _reviews.GetMyReviewsAsync(TestSupport.As(customer), new() { Page = 1, PageSize = 2 })).Value!;
        var second = (await _reviews.GetMyReviewsAsync(TestSupport.As(customer), new() { Page = 2, PageSize = 2 })).Value!;

        Assert.Equal(3, first.TotalCount);
        Assert.Equal(2, first.Items.Count);
        Assert.Equal(3, second.TotalCount);
        Assert.Single(second.Items);
        Assert.All(first.Items.Concat(second.Items), review => Assert.Equal(customer.Id, review.CustomerId));
        Assert.Equal(3, first.Items.Concat(second.Items).Select(review => review.Id).Distinct().Count());
    }

    [Fact]
    public async Task GetMyReviews_RatingFilter_AndNamesAreFilledIn()
    {
        var customer = TestSupport.NewUser();
        customer.FirstName = "Amra";
        customer.LastName = "Hadžić";
        _ctx.ApplicationUsers.Add(customer);
        await _ctx.SaveChangesAsync();
        foreach (var rating in new[] { 4, 2, 4 })
        {
            var (_, appointment) = await AddJobAsync(AppointmentStatus.Completed, customer);
            await _reviews.CreateMyReviewAsync(TestSupport.As(customer), new() { AppointmentId = appointment.Id, Rating = rating });
        }

        var fours = (await _reviews.GetMyReviewsAsync(TestSupport.As(customer), new() { Rating = 4 })).Value!;
        var fives = (await _reviews.GetMyReviewsAsync(TestSupport.As(customer), new() { Rating = 5 })).Value!;

        Assert.Equal(2, fours.TotalCount);
        Assert.All(fours.Items, review => Assert.Equal(4, review.Rating));
        Assert.All(fours.Items, review => Assert.Contains("Amra", review.CustomerName));
        Assert.All(fours.Items, review => Assert.False(string.IsNullOrWhiteSpace(review.TradespersonName)));
        Assert.Empty(fives.Items);
        Assert.Equal(0, fives.TotalCount);
    }

    [Fact]
    public async Task GetMyReviews_ForeignAppointmentFilter_IsEmpty_NotTheOtherReview()
    {
        var (customer, _) = await AddJobAsync(AppointmentStatus.Completed);
        var (other, otherAppointment) = await AddJobAsync(AppointmentStatus.Completed);
        await _reviews.CreateMyReviewAsync(TestSupport.As(other), new() { AppointmentId = otherAppointment.Id, Rating = 5 });

        var result = (await _reviews.GetMyReviewsAsync(TestSupport.As(customer), new() { AppointmentId = otherAppointment.Id })).Value!;

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    // ---------- own profile ----------

    [Theory]
    [InlineData("foo@x.test")]
    [InlineData("FOO@X.TEST")]
    [InlineData("  fOo@x.TeSt  ")]
    public async Task UpdateMyProfile_EmailOfAnotherUserInAnyCase_IsConflict(string email)
    {
        var other = TestSupport.NewUser();
        other.Email = "Foo@x.test";
        var user = TestSupport.NewUser();
        _ctx.ApplicationUsers.AddRange(other, user);
        await _ctx.SaveChangesAsync();
        var original = user.Email;

        var result = await _users.UpdateMyProfileAsync(user.Id, new() { Email = email, FirstName = "A", LastName = "B" });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _ctx.ChangeTracker.Clear();
        Assert.Equal(original, (await _ctx.ApplicationUsers.SingleAsync(u => u.Id == user.Id)).Email);
    }

    [Fact]
    public async Task UpdateMyProfile_OwnEmailInAnotherCase_Succeeds()
    {
        var user = TestSupport.NewUser();
        user.Email = "ana@x.test";
        _ctx.ApplicationUsers.Add(user);
        await _ctx.SaveChangesAsync();

        var result = await _users.UpdateMyProfileAsync(user.Id, new() { Email = "ANA@x.test", FirstName = "Ana", LastName = "B" });

        Assert.True(result.IsSuccess);
        _ctx.ChangeTracker.Clear();
        Assert.Equal("ANA@x.test", (await _ctx.ApplicationUsers.SingleAsync()).Email);
    }

    [Fact]
    public async Task UpdateMyProfile_TrimsNamesAndPhone()
    {
        var user = TestSupport.NewUser();
        _ctx.ApplicationUsers.Add(user);
        await _ctx.SaveChangesAsync();

        await _users.UpdateMyProfileAsync(user.Id, new()
        {
            Email = user.Email, FirstName = "  Amra\t", LastName = "\n Hadžić ", PhoneNumber = "  +387 61 123 456  "
        });

        _ctx.ChangeTracker.Clear();
        var stored = await _ctx.ApplicationUsers.SingleAsync();
        Assert.Equal("Amra", stored.FirstName);
        Assert.Equal("Hadžić", stored.LastName);
        Assert.Equal("+387 61 123 456", stored.PhoneNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateMyProfile_EmptyPhone_ClearsAnExistingNumber(string? phone)
    {
        var user = TestSupport.NewUser();
        user.PhoneNumber = "061 111 222";
        _ctx.ApplicationUsers.Add(user);
        await _ctx.SaveChangesAsync();

        await _users.UpdateMyProfileAsync(user.Id, new() { Email = user.Email, FirstName = "A", LastName = "B", PhoneNumber = phone });

        _ctx.ChangeTracker.Clear();
        Assert.Null((await _ctx.ApplicationUsers.SingleAsync()).PhoneNumber);
    }

    [Fact]
    public async Task UnknownUser_IsNotFound_ForProfileAndPhoto()
    {
        Assert.Equal(ErrorType.NotFound,
            (await _users.UpdateMyProfileAsync(424242, new() { Email = "a@b.test", FirstName = "A", LastName = "B" })).ErrorType);
        Assert.Equal(ErrorType.NotFound,
            (await _users.SetProfileImageAsync(424242, "Account/photos/x.png")).ErrorType);
    }

    [Fact]
    public async Task SetProfileImage_Null_ClearsAndReturnsThePrevious_ThenNullOnSecondCall()
    {
        var user = TestSupport.NewUser();
        user.ProfileImageUrl = "https://example.test/seed.jpg";
        _ctx.ApplicationUsers.Add(user);
        await _ctx.SaveChangesAsync();

        Assert.Equal("https://example.test/seed.jpg", (await _users.SetProfileImageAsync(user.Id, null)).Value);
        var second = await _users.SetProfileImageAsync(user.Id, null);
        Assert.True(second.IsSuccess);
        Assert.Null(second.Value);
    }

    // ---------- profile validator ----------

    [Theory]
    [InlineData("+387 61 123 456", true)]
    [InlineData("061/123-456", true)]
    [InlineData("123456", true)]
    [InlineData("+123456", true)]
    [InlineData("12345", false)]                     // 5 digits
    [InlineData("12345678901234567890", true)]      // 20 digits
    [InlineData("123456789012345678901", false)]    // 21 digits
    [InlineData("+12345678901234567890", true)]     // leading + and 20 digits
    [InlineData("(061) 123 456", false)]            // brackets
    [InlineData("061.123.456", false)]
    [InlineData("++38761123456", false)]
    [InlineData("387+61123456", false)]
    [InlineData("061 123 456 ext 2", false)]
    [InlineData("٠٦١١٢٣٤٥٦", false)]                // Arabic-Indic digits are not 0-9
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    public void ProfileValidator_PhoneTable(string? phone, bool isValid)
    {
        Assert.Equal(isValid, Validate(phone: phone).IsValid);
    }

    // The limit counts digits, so a value made only of separators is not a phone number.
    [Theory]
    [InlineData("------")]
    [InlineData("+ / - /")]
    public void ProfileValidator_PhoneWithoutAnyDigit_IsInvalid(string phone)
    {
        Assert.False(Validate(phone: phone).IsValid, $"'{phone}' has no digits but passes the phone rule.");
    }

    // Separators do not count toward the 20-digit limit: 13 digits in 23 characters are fine.
    [Fact]
    public void ProfileValidator_PhoneWithManySeparators_CountsDigitsNotCharacters()
    {
        Assert.True(Validate(phone: "00 387 / 61 - 123 - 456").IsValid, "13 digits are refused because separators count toward the limit of 20.");
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void ProfileValidator_NameLengths(int length, bool isValid)
    {
        Assert.Equal(isValid, Validate(first: new string('a', length)).IsValid);
        Assert.Equal(isValid, Validate(last: new string('a', length)).IsValid);
    }

    [Fact]
    public void ProfileValidator_EmailLength()
    {
        var local = new string('a', 64);
        var ok = local + "@" + new string('b', 256 - 64 - 1 - 5) + ".test";
        Assert.Equal(256, ok.Length);
        Assert.True(Validate(email: ok).IsValid);
        Assert.False(Validate(email: ok + "x").IsValid);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    public void ProfileValidator_WhitespaceNames_AreRequired(string name)
    {
        Assert.False(Validate(first: name).IsValid);
        Assert.False(Validate(last: name).IsValid);
    }

    [Theory]
    [InlineData("@x.test", false)]
    [InlineData("ana@", false)]
    [InlineData("   ", false)]
    [InlineData("ana@x.test", true)]
    public void ProfileValidator_Email(string email, bool isValid)
    {
        Assert.Equal(isValid, Validate(email: email).IsValid);
    }

    private static FluentValidation.Results.ValidationResult Validate(string? phone = null, string first = "Ime",
        string last = "Prezime", string email = "ime@primjer.ba")
        => new MyProfileUpdateRequestValidator().Validate(new MyProfileUpdateRequest
        {
            Email = email, PhoneNumber = phone, FirstName = first, LastName = last
        });

    // A job of "customer" (a new one when null) with an appointment in the given status.
    private async Task<(ApplicationUser Customer, Appointment Appointment)> AddJobAsync(AppointmentStatus status,
        ApplicationUser? customer = null)
    {
        customer ??= TestSupport.NewUser();
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
