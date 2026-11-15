using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

public class DashboardAndReportServiceTests : IDisposable
{
    private static readonly DateTime October = new(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime November = new(2026, 11, 15, 0, 0, 0, DateTimeKind.Utc);

    private readonly MajstorioDbContext _ctx;
    private readonly City _mostar = new() { Name = "Mostar" };
    private readonly City _sarajevo = new() { Name = "Sarajevo" };
    private readonly ServiceCategory _plumbing = new() { Name = "Plumbing" };
    private readonly ServiceCategory _electrical = new() { Name = "Electrical" };

    public DashboardAndReportServiceTests()
    {
        _ctx = new MajstorioDbContext(new DbContextOptionsBuilder<MajstorioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var customer = NewUser("amra", "Amra", "Customer");
        var emir = new TradespersonProfile
        {
            User = NewUser("emir", "Emir", "Kovac"), City = _mostar, IsApproved = true
        };
        var dino = new TradespersonProfile
        {
            User = NewUser("dino", "Dino", "Begic"), City = _sarajevo, IsApproved = true
        };
        // An application that waits for approval; inactive users are not counted as active.
        var applicant = new TradespersonProfile
        {
            User = NewUser("haris", "Haris", "Delic", isActive: false), City = _mostar, IsApproved = false
        };
        _ctx.TradespersonProfiles.AddRange(emir, dino, applicant);

        // Emir: two offers in October, one completed job with a 4 and a 5 star review overall.
        var completed = NewRequest(customer, _mostar, _plumbing, RequestStatus.Completed, October);
        var completedOffer = NewOffer(completed, emir, October);
        completedOffer.Appointment = new Appointment
        {
            Status = AppointmentStatus.Completed, StartUtc = October, EndUtc = October.AddHours(1)
        };
        var open = NewRequest(customer, _mostar, _plumbing, RequestStatus.Created, October);
        NewOffer(open, emir, October);
        NewRequest(customer, _mostar, _plumbing, RequestStatus.Created, November);
        NewRequest(customer, _sarajevo, _electrical, RequestStatus.Cancelled, November);

        _ctx.Reviews.AddRange(
            new Review { TradespersonProfile = emir, Customer = customer, Rating = 5, CreatedAtUtc = October,
                Appointment = completedOffer.Appointment },
            new Review { TradespersonProfile = emir, Customer = customer, Rating = 4, CreatedAtUtc = November,
                Appointment = new Appointment { Offer = NewOffer(open, dino, November), Status = AppointmentStatus.Cancelled } });
        _ctx.SaveChanges();
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task Dashboard_CountsAndGroupsRequests()
    {
        var dashboard = (await new DashboardService(_ctx).GetDashboardAsync()).Value!;

        Assert.Equal(3, dashboard.ActiveUserCount);
        Assert.Equal(1, dashboard.PendingTradespersonCount);
        Assert.Equal(2, dashboard.ActiveRequestCount);
        Assert.Equal(1, dashboard.CompletedServiceCount);
        Assert.Equal(("Plumbing", 3), (dashboard.RequestsByCategory[0].Name, dashboard.RequestsByCategory[0].Count));
        Assert.Equal(("Electrical", 1), (dashboard.RequestsByCategory[1].Name, dashboard.RequestsByCategory[1].Count));
        Assert.Equal(("Mostar", 3), (dashboard.RequestsByCity[0].Name, dashboard.RequestsByCity[0].Count));
    }

    [Fact]
    public async Task RequestReport_GroupsByCityCategoryAndStatusAndAppliesFilters()
    {
        var service = new ReportService(_ctx);

        var all = (await service.GetRequestReportAsync(new())).Value!;
        Assert.Equal(3, all.Count);
        var created = Assert.Single(all, row => row.CityName == "Mostar" && row.Status == (int)RequestStatus.Created);
        Assert.Equal("Plumbing", created.ServiceCategoryName);
        Assert.Equal(2, created.RequestCount);

        var october = (await service.GetRequestReportAsync(new()
        {
            FromUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            ToUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc)
        })).Value!;
        Assert.Equal(2, october.Sum(row => row.RequestCount));

        var cancelledInSarajevo = (await service.GetRequestReportAsync(new()
        {
            CityId = _sarajevo.Id, ServiceCategoryId = _electrical.Id, Status = (int)RequestStatus.Cancelled
        })).Value!;
        Assert.Equal(1, Assert.Single(cancelledInSarajevo).RequestCount);
    }

    [Fact]
    public async Task TradespersonReport_CountsOffersJobsAndAverageRatingOfApprovedProfiles()
    {
        var service = new ReportService(_ctx);

        var all = (await service.GetTradespersonReportAsync(new())).Value!;
        Assert.Equal(2, all.Count);
        var emir = all[0];
        Assert.Equal("Emir Kovac", emir.TradespersonName);
        Assert.Equal("Mostar", emir.CityName);
        Assert.Equal(2, emir.OfferCount);
        Assert.Equal(1, emir.CompletedJobCount);
        Assert.Equal(4.5, emir.AverageRating);
        Assert.Null(all[1].AverageRating);

        var november = (await service.GetTradespersonReportAsync(new()
        {
            FromUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), CityId = _mostar.Id
        })).Value!;
        var emirInNovember = Assert.Single(november);
        Assert.Equal(0, emirInNovember.OfferCount);
        Assert.Equal(0, emirInNovember.CompletedJobCount);
        Assert.Equal(4, emirInNovember.AverageRating);
    }

    private ServiceRequest NewRequest(
        ApplicationUser customer, City city, ServiceCategory category, RequestStatus status, DateTime createdAtUtc)
    {
        var request = new ServiceRequest
        {
            Customer = customer, City = city, ServiceCategory = category, Status = status,
            CreatedAtUtc = createdAtUtc, Description = "Repair", MaximumBudget = 100m
        };
        _ctx.ServiceRequests.Add(request);
        return request;
    }

    private Offer NewOffer(ServiceRequest request, TradespersonProfile profile, DateTime createdAtUtc)
    {
        var offer = new Offer
        {
            ServiceRequest = request, TradespersonProfile = profile, CreatedAtUtc = createdAtUtc, Price = 50m
        };
        _ctx.Offers.Add(offer);
        return offer;
    }

    private static ApplicationUser NewUser(string userName, string firstName, string lastName, bool isActive = true) => new()
    {
        UserName = userName, Email = $"{userName}@example.com", FirstName = firstName, LastName = lastName,
        PasswordHash = "hash", PasswordSalt = "salt", IsActive = isActive
    };
}
