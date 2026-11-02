using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

public class EntityNavigationTests
{
    // The only parent-child pairs where deleting the parent removes the children.
    private static readonly HashSet<(string Dependent, string Property)> CascadingForeignKeys =
    [
        (nameof(TradespersonService), nameof(TradespersonService.TradespersonProfileId)),
        (nameof(StatusHistory), nameof(StatusHistory.ServiceRequestId)),
        (nameof(Notification), nameof(Notification.ServiceRequestId)),
    ];

    private static readonly HashSet<(string Dependent, string Property)> OneToOneForeignKeys =
    [
        (nameof(TradespersonProfile), nameof(TradespersonProfile.UserId)),
        (nameof(Appointment), nameof(Appointment.OfferId)),
        (nameof(Review), nameof(Review.AppointmentId)),
        (nameof(Refund), nameof(Refund.PaymentId)),
    ];

    [Fact]
    public void OneToManyRelationships_HavePairedCollectionsWithoutExtraForeignKeys()
    {
        using var ctx = CreateContext();

        foreach (var foreignKey in ctx.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
        {
            Assert.DoesNotContain(foreignKey.Properties, property => property.IsShadowProperty());
            if (foreignKey.IsUnique)
                continue;

            Assert.NotNull(foreignKey.PrincipalToDependent);
            Assert.True(foreignKey.PrincipalToDependent.IsCollection);
            Assert.Same(foreignKey.DependentToPrincipal, foreignKey.PrincipalToDependent.Inverse);
        }
    }

    [Fact]
    public void ForeignKeys_RestrictDeleteExceptForOwnedChildren()
    {
        using var ctx = CreateContext();

        foreach (var foreignKey in ctx.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
        {
            var key = (foreignKey.DeclaringEntityType.ClrType.Name, foreignKey.Properties.Single().Name);
            var expected = CascadingForeignKeys.Contains(key) ? DeleteBehavior.Cascade : DeleteBehavior.Restrict;
            Assert.True(expected == foreignKey.DeleteBehavior,
                $"{key.Item1}.{key.Item2} should be {expected} but is {foreignKey.DeleteBehavior}.");
        }
    }

    [Fact]
    public void OneToOneRelationships_HaveUniqueForeignKeysWithReferenceNavigationsOnBothSides()
    {
        using var ctx = CreateContext();

        var uniqueKeys = ctx.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys())
            .Where(foreignKey => foreignKey.IsUnique).ToList();

        Assert.Equal(OneToOneForeignKeys.Count, uniqueKeys.Count);
        foreach (var foreignKey in uniqueKeys)
        {
            Assert.Contains((foreignKey.DeclaringEntityType.ClrType.Name, foreignKey.Properties.Single().Name), OneToOneForeignKeys);
            Assert.NotNull(foreignKey.DependentToPrincipal);
            Assert.NotNull(foreignKey.PrincipalToDependent);
            Assert.False(foreignKey.PrincipalToDependent.IsCollection);
        }
    }

    [Fact]
    public void OwnerAndApproverRelationships_StaySeparate()
    {
        using var ctx = CreateContext();
        var owner = NewUser();
        var approver = NewUser();
        var profile = new TradespersonProfile
        {
            User = owner, ApprovedByUser = approver, City = new City { Name = "Mostar" }
        };

        ctx.TradespersonProfiles.Add(profile);

        Assert.Same(profile, owner.TradespersonProfile);
        Assert.Empty(owner.ApprovedTradespersonProfiles);
        Assert.Contains(profile, approver.ApprovedTradespersonProfiles);
        Assert.Null(approver.TradespersonProfile);
        Assert.Equal(owner.Id, profile.UserId);
        Assert.Equal(approver.Id, profile.ApprovedByUserId);
    }

    [Fact]
    public async Task IncludeChain_LoadsTradespersonOffersAppointmentAndReview()
    {
        using var ctx = CreateContext();
        var city = new City { Name = "Mostar" };
        var customer = NewUser();
        var profile = new TradespersonProfile { User = NewUser(), City = city };
        var appointment = new Appointment
        {
            Status = AppointmentStatus.Completed,
            Offer = new Offer
            {
                TradespersonProfile = profile,
                ServiceRequest = new ServiceRequest
                {
                    Customer = customer, City = city,
                    ServiceCategory = new ServiceCategory { Name = "Plumbing" }
                }
            }
        };
        ctx.Reviews.Add(new Review
        {
            Appointment = appointment, TradespersonProfile = profile, Customer = customer, Rating = 5, Comment = "Good work."
        });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var loadedProfile = await ctx.TradespersonProfiles
            .Include(tradesperson => tradesperson.Offers)
                .ThenInclude(offer => offer.Appointment)
                    .ThenInclude(booking => booking!.Review)
            .Include(tradesperson => tradesperson.Reviews)
            .SingleAsync();

        var loadedOffer = Assert.Single(loadedProfile.Offers);
        var loadedAppointment = loadedOffer.Appointment;
        Assert.NotNull(loadedAppointment);
        var loadedReview = loadedAppointment.Review;
        Assert.NotNull(loadedReview);
        Assert.Equal(5, loadedReview.Rating);
        Assert.Equal("Good work.", loadedReview.Comment);
        Assert.Same(loadedProfile, loadedOffer.TradespersonProfile);
        Assert.Same(loadedOffer, loadedAppointment.Offer);
        Assert.Same(loadedAppointment, loadedReview.Appointment);
        Assert.Same(loadedReview, Assert.Single(loadedProfile.Reviews));
        Assert.Equal(customer.Id, loadedReview.CustomerId);
    }

    private static MajstorioDbContext CreateContext() => new(new DbContextOptionsBuilder<MajstorioDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ApplicationUser NewUser() => new()
    {
        UserName = Guid.NewGuid().ToString(), PasswordHash = "test-hash", PasswordSalt = "test-salt"
    };
}
