using Majstorio.Services.Auth;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Mapping;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Majstorio.Services.Tests;

internal static class TestSupport
{
    // The in-memory provider has no transactions; the services still open one, so that warning is ignored.
    public static MajstorioDbContext NewInMemoryContext() => new(new DbContextOptionsBuilder<MajstorioDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

    public static IMapper NewMapper()
    {
        var config = new TypeAdapterConfig();
        new OfferMappingProfile().Register(config);
        new AppointmentMappingProfile().Register(config);
        new ServiceRequestMappingProfile().Register(config);
        new UserMappingProfile().Register(config);
        return new Mapper(config);
    }

    public static ApplicationUser NewUser(UserRole role = UserRole.Customer)
    {
        var name = Guid.NewGuid().ToString("N");
        return new ApplicationUser
        {
            UserName = name, Email = $"{name}@majstorio.test", FirstName = "Test", LastName = name[..6],
            PasswordHash = "test-hash", PasswordSalt = "test-salt", Role = role
        };
    }

    public static CurrentUser As(ApplicationUser user) => new(user.Id, user.Role);
}

// One city, one category, a customer with an open request and an approved tradesperson who matches it.
// Names are random, so the same data can also be added to a migrated SQL Server database next to the seed.
internal sealed class Scenario
{
    public MajstorioDbContext Ctx { get; }
    public City City { get; }
    public ServiceCategory Category { get; }
    public ApplicationUser Customer { get; }
    public ApplicationUser Administrator { get; }
    public TradespersonProfile Profile { get; }
    public ServiceRequest Request { get; }
    public DateTime Start { get; } = DateTime.UtcNow.AddDays(2);

    public Scenario(MajstorioDbContext ctx)
    {
        Ctx = ctx;
        City = new City { Name = "City " + Guid.NewGuid().ToString("N") };
        Category = new ServiceCategory { Name = "Category " + Guid.NewGuid().ToString("N") };
        Customer = TestSupport.NewUser();
        Administrator = TestSupport.NewUser(UserRole.Administrator);
        ctx.AddRange(City, Category, Customer, Administrator);
        ctx.SaveChanges();
        Profile = AddProfile();
        Request = AddRequest();
    }

    public TradespersonProfile AddProfile(City? city = null, ServiceCategory? category = null, bool isApproved = true)
    {
        var profile = new TradespersonProfile
        {
            User = TestSupport.NewUser(isApproved ? UserRole.Tradesperson : UserRole.Customer),
            City = city ?? City, Description = "Test profile", IsApproved = isApproved
        };
        Ctx.TradespersonServices.Add(new TradespersonService
        {
            TradespersonProfile = profile, ServiceCategory = category ?? Category, Name = "Repairs",
            Description = "Repairs", MinimumPrice = 20m, MaximumPrice = 200m, EstimatedDurationMinutes = 60
        });
        Ctx.SaveChanges();
        return profile;
    }

    public ServiceRequest AddRequest(ApplicationUser? customer = null, City? city = null, ServiceCategory? category = null)
    {
        var request = new ServiceRequest
        {
            Customer = customer ?? Customer, City = city ?? City, ServiceCategory = category ?? Category,
            Description = "Leaking tap", DesiredStartUtc = Start, MaximumBudget = 500m
        };
        Ctx.ServiceRequests.Add(request);
        Ctx.SaveChanges();
        return request;
    }

    public Offer AddOffer(TradespersonProfile? profile = null, ServiceRequest? request = null,
        OfferStatus status = OfferStatus.Pending, DateTime? start = null)
    {
        var offer = new Offer
        {
            ServiceRequest = request ?? Request, TradespersonProfile = profile ?? Profile,
            Price = 100m, ProposedStartUtc = start ?? Start, DurationMinutes = 60, Status = status
        };
        Ctx.Offers.Add(offer);
        Ctx.SaveChanges();
        return offer;
    }
}
