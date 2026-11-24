using Majstorio.Model.Results;
using Majstorio.Services.Auth;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

// Runs against a real SQL Server, because the in-memory provider has no transactions, locks or unique indexes.
// Uses LocalDB (installed with Visual Studio) or the server in MAJSTORIO_TEST_SQLSERVER; skipped when neither answers.
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (!SqlServerTestDatabase.IsAvailable.Value)
            Skip = "No SQL Server available. Install LocalDB or set MAJSTORIO_TEST_SQLSERVER to a server connection string.";
    }
}

// Creates a throwaway database with the real migrations and drops it after the tests.
public sealed class SqlServerTestDatabase : IAsyncLifetime
{
    private static readonly string Server = Environment.GetEnvironmentVariable("MAJSTORIO_TEST_SQLSERVER")
        ?? @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";

    public static readonly Lazy<bool> IsAvailable = new(() =>
    {
        try
        {
            using var connection = new SqlConnection(Server + ";Connect Timeout=30");
            connection.Open();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    });

    private readonly string _connectionString = $"{Server};Database=Majstorio_Tests_{Guid.NewGuid():N}";

    public MajstorioDbContext NewContext() => new(new DbContextOptionsBuilder<MajstorioDbContext>()
        .UseSqlServer(_connectionString).Options);

    public async Task InitializeAsync()
    {
        if (!IsAvailable.Value)
            return;

        await using var ctx = NewContext();
        await ctx.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (!IsAvailable.Value)
            return;

        await using var ctx = NewContext();
        await ctx.Database.EnsureDeletedAsync();
    }
}

public class ConcurrentAcceptTests : IClassFixture<SqlServerTestDatabase>
{
    private const int Rounds = 5;

    private readonly SqlServerTestDatabase _database;

    public ConcurrentAcceptTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task TheSameOfferAcceptedTwiceAtOnce_IsAcceptedExactlyOnce()
    {
        for (var round = 0; round < Rounds; round++)
        {
            int requestId, offerId;
            CurrentUser customer;
            await using (var ctx = _database.NewContext())
            {
                var scenario = new Scenario(ctx);
                offerId = scenario.AddOffer().Id;
                requestId = scenario.Request.Id;
                customer = TestSupport.As(scenario.Customer);
            }

            var results = await AcceptAtOnceAsync((customer, offerId), (customer, offerId));

            AssertOneWinner(results);
            await AssertAcceptedOnceAsync(requestId);
        }
    }

    [SqlServerFact]
    public async Task TwoOffersOfTheSameRequestAcceptedAtOnce_OnlyOneIsAccepted()
    {
        for (var round = 0; round < Rounds; round++)
        {
            int requestId, firstOfferId, secondOfferId;
            CurrentUser customer;
            await using (var ctx = _database.NewContext())
            {
                var scenario = new Scenario(ctx);
                firstOfferId = scenario.AddOffer().Id;
                secondOfferId = scenario.AddOffer(scenario.AddProfile()).Id;
                requestId = scenario.Request.Id;
                customer = TestSupport.As(scenario.Customer);
            }

            var results = await AcceptAtOnceAsync((customer, firstOfferId), (customer, secondOfferId));

            AssertOneWinner(results);
            await AssertAcceptedOnceAsync(requestId);
            await using var check = _database.NewContext();
            var statuses = await check.Offers.Where(offer => offer.ServiceRequestId == requestId)
                .Select(offer => offer.Status).OrderBy(status => status).ToListAsync();
            Assert.Equal([OfferStatus.Accepted, OfferStatus.Closed], statuses);
        }
    }

    [SqlServerFact]
    public async Task OverlappingOffersOfOneTradespersonAcceptedAtOnceByTwoCustomers_OnlyOneAppointmentIsCreated()
    {
        for (var round = 0; round < Rounds; round++)
        {
            int profileId, firstOfferId, secondOfferId;
            CurrentUser firstCustomer, secondCustomer;
            await using (var ctx = _database.NewContext())
            {
                var scenario = new Scenario(ctx);
                var other = TestSupport.NewUser();
                var otherRequest = scenario.AddRequest(other);
                firstOfferId = scenario.AddOffer().Id;
                secondOfferId = scenario.AddOffer(request: otherRequest, start: scenario.Start.AddMinutes(30)).Id;
                profileId = scenario.Profile.Id;
                firstCustomer = TestSupport.As(scenario.Customer);
                secondCustomer = TestSupport.As(other);
            }

            var results = await AcceptAtOnceAsync((firstCustomer, firstOfferId), (secondCustomer, secondOfferId));

            AssertOneWinner(results);
            await using var check = _database.NewContext();
            Assert.Equal(1, await check.Appointments.CountAsync(appointment => appointment.Offer.TradespersonProfileId == profileId));
            Assert.Equal(1, await check.Offers.CountAsync(offer =>
                offer.TradespersonProfileId == profileId && offer.Status == OfferStatus.Accepted));
        }
    }

    [SqlServerFact]
    public async Task UniqueIndexes_RejectASecondOfferAndASecondAcceptedOffer()
    {
        // The database refuses these rows even if a code path forgot to check.
        await using var ctx = _database.NewContext();
        var scenario = new Scenario(ctx);
        scenario.AddOffer(status: OfferStatus.Accepted);
        var otherProfileId = scenario.AddProfile().Id;

        ctx.Offers.Add(new Offer
        {
            ServiceRequestId = scenario.Request.Id, TradespersonProfileId = scenario.Profile.Id,
            Price = 50m, ProposedStartUtc = scenario.Start, DurationMinutes = 30
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
        ctx.ChangeTracker.Clear();

        ctx.Offers.Add(new Offer
        {
            ServiceRequestId = scenario.Request.Id, TradespersonProfileId = otherProfileId,
            Price = 50m, ProposedStartUtc = scenario.Start, DurationMinutes = 30, Status = OfferStatus.Accepted
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    // Every call gets its own context and connection, like two HTTP requests, and they start together.
    private async Task<Result[]> AcceptAtOnceAsync(params (CurrentUser User, int OfferId)[] calls)
    {
        using var start = new Barrier(calls.Length);
        return await Task.WhenAll(calls.Select(call => Task.Run(async () =>
        {
            await using var ctx = _database.NewContext();
            var service = new OfferService(ctx, TestSupport.NewMapper(), new FakeMessagePublisher());
            start.SignalAndWait(TimeSpan.FromSeconds(30));
            return await service.AcceptOfferAsync(call.User, call.OfferId);
        })));
    }

    private static void AssertOneWinner(Result[] results)
    {
        Assert.Single(results, result => result.IsSuccess);
        Assert.Single(results, result => result.ErrorType == ErrorType.Conflict);
    }

    private async Task AssertAcceptedOnceAsync(int requestId)
    {
        await using var ctx = _database.NewContext();
        Assert.Equal(RequestStatus.AppointmentConfirmed,
            (await ctx.ServiceRequests.SingleAsync(request => request.Id == requestId)).Status);
        Assert.Equal(1, await ctx.Offers.CountAsync(offer =>
            offer.ServiceRequestId == requestId && offer.Status == OfferStatus.Accepted));
        Assert.Equal(1, await ctx.Appointments.CountAsync(appointment => appointment.Offer.ServiceRequestId == requestId));
        Assert.Equal(2, await ctx.StatusHistories.CountAsync(history => history.ServiceRequestId == requestId));
    }
}
