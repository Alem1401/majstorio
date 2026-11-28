using Majstorio.Services.Constants;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Recommendations;
using Majstorio.Services.Services;
using Xunit;

namespace Majstorio.Services.Tests;

public class RecommenderTests : IDisposable
{
    private readonly MajstorioDbContext _ctx = TestSupport.NewInMemoryContext();
    private readonly Scenario _s;
    private readonly TradespersonProfileService _service;

    public RecommenderTests()
    {
        _s = new Scenario(_ctx);
        _service = new TradespersonProfileService(_ctx);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void CosineSimilarity_MeasuresTheAngleBetweenUserAndItemVectors()
    {
        var user = new Dictionary<int, double> { [1] = 3, [2] = 4 };

        Assert.Equal(0.8, ContentBasedRecommender.CosineSimilarity(user, [2]), 6);
        Assert.Equal(7 / (5 * Math.Sqrt(2)), ContentBasedRecommender.CosineSimilarity(user, [1, 2]), 6);
        Assert.Equal(0, ContentBasedRecommender.CosineSimilarity(user, [3]));
        Assert.Equal(0, ContentBasedRecommender.CosineSimilarity(user, []));
        Assert.Equal(0, ContentBasedRecommender.CosineSimilarity(new Dictionary<int, double>(), [1]));
    }

    [Fact]
    public void Rank_WithHistory_WeighsCategoryCityAndRating_StrongestReasonFirst()
    {
        var preferences = new UserPreferences();
        preferences.Add(categoryId: 1, cityId: 1, weight: 1);

        var ranked = ContentBasedRecommender.Rank(preferences,
        [
            new(10, CityId: 2, CategoryIds: [2], AverageRating: 5, CompletedJobs: 50),
            new(11, CityId: 1, CategoryIds: [1], AverageRating: 4, CompletedJobs: 0)
        ]);

        Assert.Equal([11, 10], ranked.Select(recommendation => recommendation.ProfileId));
        Assert.Equal(0.5 + 0.3 + 0.2 * 0.8, ranked[0].Score, 6);
        Assert.Equal([RecommendationReasons.Category, RecommendationReasons.City, RecommendationReasons.Rating],
            ranked[0].Reasons);
        // Nothing matches, so only the rating is claimed.
        Assert.Equal([RecommendationReasons.Rating], ranked[1].Reasons);
    }

    [Fact]
    public void Rank_WithoutHistory_OrdersByCompletedJobsAndRating()
    {
        var ranked = ContentBasedRecommender.Rank(new UserPreferences(),
        [
            new(1, CityId: 1, CategoryIds: [1], AverageRating: 5, CompletedJobs: 1),
            new(2, CityId: 1, CategoryIds: [1], AverageRating: 4.5, CompletedJobs: 20),
            new(3, CityId: 1, CategoryIds: [1], AverageRating: null, CompletedJobs: 0)
        ]);

        Assert.Equal([2, 1, 3], ranked.Select(recommendation => recommendation.ProfileId));
        Assert.Equal([RecommendationReasons.Popularity, RecommendationReasons.Rating], ranked[0].Reasons);
        Assert.Empty(ranked[2].Reasons);
    }

    [Fact]
    public async Task List_RanksByTheCustomersSearchHistory()
    {
        var customer = TestSupport.NewUser();
        var wantedCategory = new ServiceCategory { Name = "Wanted" };
        var match = _s.AddProfile(category: wantedCategory);
        _ctx.Add(customer);
        await _ctx.SaveChangesAsync();
        await _service.GetProfilesAsync(TestSupport.As(customer), new() { ServiceCategoryId = wantedCategory.Id });

        var result = (await _service.GetProfilesAsync(TestSupport.As(customer), new())).Value!;

        Assert.Equal([match.Id, _s.Profile.Id], result.Items.Select(profile => profile.Id));
        Assert.Equal([RecommendationReasons.Category], result.Items[0].RecommendationReasons);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task List_RanksByTheCustomersRequests_AndIgnoresCancelledOnes()
    {
        var customer = TestSupport.NewUser();
        var wanted = new ServiceCategory { Name = "Wanted" };
        var cancelled = new ServiceCategory { Name = "Cancelled" };
        var cancelledMatch = _s.AddProfile(category: cancelled);
        var match = _s.AddProfile(category: wanted);
        _s.AddRequest(customer, category: wanted);
        _s.AddRequest(customer, category: cancelled).Status = RequestStatus.Cancelled;
        await _ctx.SaveChangesAsync();

        var result = (await _service.GetProfilesAsync(TestSupport.As(customer), new())).Value!;

        Assert.Equal(match.Id, result.Items[0].Id);
        Assert.DoesNotContain(RecommendationReasons.Category,
            result.Items.Single(profile => profile.Id == cancelledMatch.Id).RecommendationReasons);
    }

    [Fact]
    public async Task List_SearchDoesNotInfluenceItsOwnRanking_SoPagesStayStable()
    {
        var customer = TestSupport.NewUser();
        var popular = _s.AddProfile();
        _ctx.Add(customer);
        var offer = _s.AddOffer(popular, status: OfferStatus.Accepted);
        _ctx.Appointments.Add(new Appointment
        {
            Offer = offer, StartUtc = _s.Start, EndUtc = _s.Start.AddHours(1), Status = AppointmentStatus.Completed
        });
        _ctx.Reviews.Add(new Review { AppointmentId = 1, TradespersonProfile = popular, Customer = _s.Customer, Rating = 4 });
        await _ctx.SaveChangesAsync();
        var search = new Majstorio.Model.Requests.TradespersonProfile.TradespersonProfileSearchRequest
        {
            ServiceCategoryId = _s.Category.Id, PageSize = 1
        };

        var first = (await _service.GetProfilesAsync(TestSupport.As(customer), search)).Value!;
        search.Page = 2;
        var second = (await _service.GetProfilesAsync(TestSupport.As(customer), search)).Value!;

        Assert.Equal(popular.Id, Assert.Single(first.Items).Id);
        Assert.Contains(RecommendationReasons.Popularity, first.Items[0].RecommendationReasons);
        Assert.Equal(_s.Profile.Id, Assert.Single(second.Items).Id);
    }

    [Fact]
    public async Task List_WithoutFilters_WritesNoSearchLog()
    {
        await _service.GetProfilesAsync(TestSupport.As(_s.Customer), new());

        Assert.Empty(_ctx.SearchLogs);
    }

    [Fact]
    public async Task List_ForAdministrators_GivesNoRecommendationReasons()
    {
        var result = (await _service.GetProfilesAsync(TestSupport.As(_s.Administrator), new())).Value!;

        Assert.Empty(Assert.Single(result.Items).RecommendationReasons);
    }
}

// The in-memory provider runs any LINQ; only a real server proves the recommender's query translates to SQL.
public class RecommenderSqlServerTests : IClassFixture<SqlServerTestDatabase>
{
    private readonly SqlServerTestDatabase _database;

    public RecommenderSqlServerTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task List_ForACustomer_RunsOnSqlServer()
    {
        await using var ctx = _database.NewContext();
        var scenario = new Scenario(ctx);
        var service = new TradespersonProfileService(ctx);

        var result = (await service.GetProfilesAsync(TestSupport.As(scenario.Customer), new()
        {
            CityId = scenario.City.Id, ServiceCategoryId = scenario.Category.Id
        })).Value!;

        var profile = Assert.Single(result.Items);
        Assert.Equal(scenario.Profile.Id, profile.Id);
        // The scenario's open request is the customer's history.
        Assert.Equal([RecommendationReasons.Category, RecommendationReasons.City], profile.RecommendationReasons);
    }
}
