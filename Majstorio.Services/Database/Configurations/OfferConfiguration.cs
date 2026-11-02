using Majstorio.Services.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Majstorio.Services.Database.Configurations;

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.Property(offer => offer.Price).HasPrecision(ColumnLengths.MoneyPrecision, ColumnLengths.MoneyScale);
        builder.Property(offer => offer.DecisionReason).HasMaxLength(ColumnLengths.LongText);

        builder.HasOne(offer => offer.ServiceRequest)
            .WithMany(request => request.Offers)
            .HasForeignKey(offer => offer.ServiceRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(offer => offer.TradespersonProfile)
            .WithMany(profile => profile.Offers)
            .HasForeignKey(offer => offer.TradespersonProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(offer => offer.DecidedByUser)
            .WithMany(user => user.DecidedOffers)
            .HasForeignKey(offer => offer.DecidedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // A tradesperson sends one offer per request.
        builder.HasIndex(offer => new { offer.ServiceRequestId, offer.TradespersonProfileId }).IsUnique();

        // A request can have only one accepted offer.
        builder.HasIndex(offer => offer.ServiceRequestId)
            .IsUnique()
            .HasDatabaseName("IX_Offers_ServiceRequestId_Accepted")
            .HasFilter($"[Status] = {(int)OfferStatus.Accepted}");
    }
}
