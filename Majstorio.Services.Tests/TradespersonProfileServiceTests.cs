using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Services;
using Xunit;

namespace Majstorio.Services.Tests;

public class TradespersonProfileServiceTests : IDisposable
{
    private readonly MajstorioDbContext _ctx = TestSupport.NewInMemoryContext();
    private readonly Scenario _s;
    private readonly TradespersonProfileService _service;

    public TradespersonProfileServiceTests()
    {
        _s = new Scenario(_ctx);
        _service = new TradespersonProfileService(_ctx);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task List_HidesUnapprovedAndInactiveProfilesFromEveryoneButAdministrators()
    {
        var applicant = _s.AddProfile(isApproved: false);
        var inactive = _s.AddProfile();
        inactive.IsActive = false;
        await _ctx.SaveChangesAsync();

        // A customer cannot widen the list with the administrator filters.
        var forCustomer = (await _service.GetProfilesAsync(TestSupport.As(_s.Customer), new() { IsApproved = false })).Value!;
        var forAdministrator = (await _service.GetProfilesAsync(TestSupport.As(_s.Administrator), new())).Value!;
        var applicants = (await _service.GetProfilesAsync(TestSupport.As(_s.Administrator), new() { IsApproved = false })).Value!;

        Assert.Equal(_s.Profile.Id, Assert.Single(forCustomer.Items).Id);
        Assert.Equal(3, forAdministrator.TotalCount);
        Assert.Equal(applicant.Id, Assert.Single(applicants.Items).Id);
    }

    [Fact]
    public async Task List_FiltersByCityCategoryAndRating_AndReturnsRatingAndPriceRange()
    {
        var otherCity = new City { Name = "Other city" };
        var otherCategory = new ServiceCategory { Name = "Other category" };
        var elsewhere = _s.AddProfile(city: otherCity);
        var otherTrade = _s.AddProfile(category: otherCategory);
        AddReview(_s.Profile, 1, 5);
        AddReview(_s.Profile, 2, 4);
        AddReview(elsewhere, 3, 2);
        await _ctx.SaveChangesAsync();
        var customer = TestSupport.As(_s.Customer);

        var byCity = (await _service.GetProfilesAsync(customer, new() { CityId = otherCity.Id })).Value!;
        var byCategory = (await _service.GetProfilesAsync(customer, new() { ServiceCategoryId = otherCategory.Id })).Value!;
        var byRating = (await _service.GetProfilesAsync(customer, new() { MinimumRating = 4 })).Value!;
        var all = (await _service.GetProfilesAsync(customer, new())).Value!;

        Assert.Equal(elsewhere.Id, Assert.Single(byCity.Items).Id);
        Assert.Equal(otherTrade.Id, Assert.Single(byCategory.Items).Id);
        var best = Assert.Single(byRating.Items);
        Assert.Equal(_s.Profile.Id, best.Id);
        Assert.Equal(4.5, best.AverageRating);
        Assert.Equal(2, best.ReviewCount);
        Assert.Equal(20m, best.MinimumPrice);
        Assert.Equal(200m, best.MaximumPrice);
        Assert.Equal([_s.Category.Name], best.Categories);
        Assert.Equal(_s.City.Name, best.CityName);
        // Best rated first, profiles without reviews last.
        Assert.Equal([_s.Profile.Id, elsewhere.Id, otherTrade.Id], all.Items.Select(profile => profile.Id));
    }

    [Fact]
    public async Task List_FilteredByAvailability_HidesBusyTradespeopleAndKeepsFreeOnes()
    {
        var free = _s.AddProfile();
        var offer = _s.AddOffer(status: OfferStatus.Accepted);
        _ctx.Appointments.Add(new Appointment
        {
            Offer = offer, StartUtc = _s.Start, EndUtc = _s.Start.AddHours(1), Status = AppointmentStatus.Confirmed
        });
        await _ctx.SaveChangesAsync();

        var result = (await _service.GetProfilesAsync(TestSupport.As(_s.Customer), new()
        {
            AvailableFromUtc = _s.Start.AddMinutes(-30), AvailableToUtc = _s.Start.AddMinutes(30)
        })).Value!;

        Assert.Equal(free.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task Detail_ReturnsServicesAndLatestReviews()
    {
        AddReview(_s.Profile, 1, 5);
        await _ctx.SaveChangesAsync();

        var detail = (await _service.GetProfileByIdAsync(TestSupport.As(_s.Customer), _s.Profile.Id)).Value!;

        Assert.Equal("Repairs", Assert.Single(detail.Services).Name);
        Assert.Equal(_s.Category.Name, detail.Services[0].ServiceCategoryName);
        Assert.Equal(5, Assert.Single(detail.LatestReviews).Rating);
        Assert.Equal(5, detail.AverageRating);
        Assert.Equal(1, detail.ReviewCount);
    }

    [Fact]
    public async Task Detail_OfAnUnapprovedProfile_IsVisibleOnlyToItsOwnerAndAdministrators()
    {
        var applicant = _s.AddProfile(isApproved: false);

        Assert.Equal(ErrorType.NotFound,
            (await _service.GetProfileByIdAsync(TestSupport.As(_s.Customer), applicant.Id)).ErrorType);
        Assert.True((await _service.GetProfileByIdAsync(TestSupport.As(applicant.User), applicant.Id)).IsSuccess);
        Assert.True((await _service.GetProfileByIdAsync(TestSupport.As(_s.Administrator), applicant.Id)).IsSuccess);
    }

    [Fact]
    public async Task Update_ChangesCityAndDescription()
    {
        var city = new City { Name = "New city" };
        _ctx.Add(city);
        await _ctx.SaveChangesAsync();

        var result = await _service.UpdateProfileAsync(new()
        {
            Id = _s.Profile.Id, CityId = city.Id, Description = "  Electrician "
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(city.Id, _s.Profile.CityId);
        Assert.Equal("Electrician", _s.Profile.Description);
        Assert.True(_s.Profile.IsApproved);
    }

    [Fact]
    public async Task Update_MissingProfileOrInactiveCity_IsRejected_ButTheCurrentCityMayStay()
    {
        var inactive = new City { Name = "Inactive", IsActive = false };
        _ctx.Add(inactive);
        _s.City.IsActive = false;
        await _ctx.SaveChangesAsync();

        var missing = await _service.UpdateProfileAsync(new() { Id = 999, CityId = _s.City.Id, Description = "Text" });
        var moved = await _service.UpdateProfileAsync(new() { Id = _s.Profile.Id, CityId = inactive.Id, Description = "Text" });
        var kept = await _service.UpdateProfileAsync(new() { Id = _s.Profile.Id, CityId = _s.City.Id, Description = "Text" });

        Assert.Equal(ErrorType.NotFound, missing.ErrorType);
        Assert.Equal(ErrorType.NotFound, moved.ErrorType);
        Assert.True(kept.IsSuccess);
    }

    [Fact]
    public async Task Approve_GrantsTheTradespersonRoleRecordsTheAdministratorAndInvalidatesOldTokens()
    {
        var applicant = _s.AddProfile(isApproved: false);
        var tokenVersion = applicant.User.TokenVersion;
        Assert.Equal(UserRole.Customer, applicant.User.Role);

        var result = await _service.ApproveProfileAsync(TestSupport.As(_s.Administrator), applicant.Id);

        Assert.True(result.IsSuccess);
        Assert.True(applicant.IsApproved);
        Assert.Equal(_s.Administrator.Id, applicant.ApprovedByUserId);
        Assert.NotNull(applicant.ApprovedAtUtc);
        Assert.Equal(UserRole.Tradesperson, applicant.User.Role);
        Assert.Equal(tokenVersion + 1, applicant.User.TokenVersion);
    }

    [Fact]
    public async Task Approve_TwiceOrMissingProfile_IsRejected()
    {
        var administrator = TestSupport.As(_s.Administrator);

        Assert.Equal(ErrorType.Conflict, (await _service.ApproveProfileAsync(administrator, _s.Profile.Id)).ErrorType);
        Assert.Equal(ErrorType.NotFound, (await _service.ApproveProfileAsync(administrator, 999)).ErrorType);
    }

    [Fact]
    public async Task Deactivate_HidesTheProfileAndBlocksNewOffers_ActivateRestoresIt()
    {
        Assert.True((await _service.SetProfileActiveAsync(new() { Id = _s.Profile.Id, IsActive = false })).IsSuccess);

        Assert.Empty((await _service.GetProfilesAsync(TestSupport.As(_s.Customer), new())).Value!.Items);
        var offers = new OfferService(_ctx, TestSupport.NewMapper(), new FakeMessagePublisher());
        var offer = await offers.CreateOfferAsync(TestSupport.As(_s.Profile.User), new()
        {
            ServiceRequestId = _s.Request.Id, Price = 50m, ProposedStartUtc = _s.Start, DurationMinutes = 60
        });
        Assert.Equal(ErrorType.Forbidden, offer.ErrorType);

        Assert.True((await _service.SetProfileActiveAsync(new() { Id = _s.Profile.Id, IsActive = true })).IsSuccess);
        Assert.Single((await _service.GetProfilesAsync(TestSupport.As(_s.Customer), new())).Value!.Items);
        Assert.Equal(ErrorType.NotFound, (await _service.SetProfileActiveAsync(new() { Id = 999 })).ErrorType);
    }

    [Fact]
    public async Task List_FirstPageOfACustomerSearch_WritesOneSearchLog()
    {
        await _service.GetProfilesAsync(TestSupport.As(_s.Customer), new()
        {
            Name = "  Marko ", CityId = _s.City.Id, ServiceCategoryId = _s.Category.Id, MinimumRating = 4,
            AvailableFromUtc = _s.Start, AvailableToUtc = _s.Start.AddDays(1)
        });

        var log = Assert.Single(_ctx.SearchLogs);
        Assert.Equal(_s.Customer.Id, log.UserId);
        Assert.Equal(_s.City.Id, log.CityId);
        Assert.Equal(_s.Category.Id, log.ServiceCategoryId);
        Assert.Equal("Marko", log.Query);
        Assert.Equal(4m, log.MinimumRating);
        Assert.Equal(_s.Start, log.AvailableFromUtc);
        Assert.Equal(_s.Start.AddDays(1), log.AvailableToUtc);
    }

    [Fact]
    public async Task List_SecondPageOrAdministratorSearch_WritesNoSearchLog()
    {
        await _service.GetProfilesAsync(TestSupport.As(_s.Customer), new() { Page = 2 });
        await _service.GetProfilesAsync(TestSupport.As(_s.Administrator), new());

        Assert.Empty(_ctx.SearchLogs);
    }

    private void AddReview(TradespersonProfile profile, int appointmentId, int rating) => _ctx.Reviews.Add(new Review
    {
        AppointmentId = appointmentId, TradespersonProfile = profile, Customer = _s.Customer, Rating = rating
    });
}
