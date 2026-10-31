using Majstorio.Model.Requests.Offer;
using Majstorio.Services.Enums;
using Majstorio.Services.Validators.Offer;
using Xunit;

namespace Majstorio.Services.Tests;

public class OfferRequestValidatorTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.001")]
    [InlineData("10000000000000000")]
    public void Validators_RejectNonPositiveOrUnsupportedPrices(string value)
    {
        var price = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var create = CreateRequest();
        create.Price = price;
        var update = UpdateRequest();
        update.Price = price;

        Assert.Contains(new OfferCreateRequestValidator().Validate(create).Errors,
            error => error.PropertyName == nameof(create.Price));
        Assert.Contains(new OfferUpdateRequestValidator().Validate(update).Errors,
            error => error.PropertyName == nameof(update.Price));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validators_RejectNonPositiveDurations(int duration)
    {
        var create = CreateRequest();
        create.DurationMinutes = duration;
        var update = UpdateRequest();
        update.DurationMinutes = duration;

        Assert.Contains(new OfferCreateRequestValidator().Validate(create).Errors,
            error => error.PropertyName == nameof(create.DurationMinutes));
        Assert.Contains(new OfferUpdateRequestValidator().Validate(update).Errors,
            error => error.PropertyName == nameof(update.DurationMinutes));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Validators_RejectNonUtcProposedStart(DateTimeKind kind)
    {
        var create = CreateRequest();
        create.ProposedStartUtc = DateTime.SpecifyKind(create.ProposedStartUtc, kind);
        var update = UpdateRequest();
        update.ProposedStartUtc = create.ProposedStartUtc;

        Assert.Contains(new OfferCreateRequestValidator().Validate(create).Errors,
            error => error.PropertyName == nameof(create.ProposedStartUtc));
        Assert.Contains(new OfferUpdateRequestValidator().Validate(update).Errors,
            error => error.PropertyName == nameof(update.ProposedStartUtc));
    }

    [Fact]
    public void Validators_RejectMissingProposedStart()
    {
        var create = CreateRequest();
        create.ProposedStartUtc = default;
        var update = UpdateRequest();
        update.ProposedStartUtc = default;

        Assert.Contains(new OfferCreateRequestValidator().Validate(create).Errors,
            error => error.PropertyName == nameof(create.ProposedStartUtc));
        Assert.Contains(new OfferUpdateRequestValidator().Validate(update).Errors,
            error => error.PropertyName == nameof(update.ProposedStartUtc));
    }

    [Fact]
    public void Validators_RejectPastStartForCreationEditingAndAcceptance()
    {
        var create = CreateRequest();
        create.ProposedStartUtc = DateTime.UtcNow.AddHours(-1);
        Assert.Contains(new OfferCreateRequestValidator().Validate(create).Errors,
            error => error.PropertyName == nameof(create.ProposedStartUtc));

        var update = UpdateRequest();
        update.ProposedStartUtc = create.ProposedStartUtc;
        foreach (var status in new[] { OfferStatus.Pending, OfferStatus.Accepted })
        {
            update.Status = (int)status;
            update.DecidedByUserId = status == OfferStatus.Pending ? null : 1;
            Assert.Contains(new OfferUpdateRequestValidator().Validate(update).Errors,
                error => error.PropertyName == nameof(update.ProposedStartUtc));
        }
    }

    [Theory]
    [InlineData(OfferStatus.Rejected)]
    [InlineData(OfferStatus.Withdrawn)]
    public void UpdateValidator_AllowsRejectingOrWithdrawingAnExpiredOffer(OfferStatus status)
    {
        var request = UpdateRequest();
        request.ProposedStartUtc = DateTime.UtcNow.AddHours(-1);
        request.Status = (int)status;
        request.DecidedByUserId = 1;

        Assert.True(new OfferUpdateRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Validators_RejectDurationThatWouldOverflowEndDate()
    {
        var create = CreateRequest();
        create.ProposedStartUtc = DateTime.SpecifyKind(DateTime.MaxValue.AddMinutes(-1), DateTimeKind.Utc);
        create.DurationMinutes = 2;
        var update = UpdateRequest();
        update.ProposedStartUtc = create.ProposedStartUtc;
        update.DurationMinutes = create.DurationMinutes;

        Assert.Contains(new OfferCreateRequestValidator().Validate(create).Errors,
            error => error.PropertyName == nameof(create.DurationMinutes));
        Assert.Contains(new OfferUpdateRequestValidator().Validate(update).Errors,
            error => error.PropertyName == nameof(update.DurationMinutes));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateValidator_RequiresPositiveDecisionUserId(int? actorId)
    {
        var request = UpdateRequest();
        request.Status = (int)OfferStatus.Accepted;
        request.DecidedByUserId = actorId;

        Assert.Contains(new OfferUpdateRequestValidator().Validate(request).Errors,
            error => error.PropertyName == nameof(request.DecidedByUserId));
    }

    [Fact]
    public void UpdateValidator_RejectsInvalidStatusAndDecisionMetadataWhilePending()
    {
        var request = UpdateRequest();
        request.Status = 999;
        Assert.Contains(new OfferUpdateRequestValidator().Validate(request).Errors,
            error => error.PropertyName == nameof(request.Status));

        request.Status = (int)OfferStatus.Pending;
        request.DecidedByUserId = 1;
        request.DecisionReason = "A decision";
        var errors = new OfferUpdateRequestValidator().Validate(request).Errors;
        Assert.Contains(errors, error => error.PropertyName == nameof(request.DecidedByUserId));
        Assert.Contains(errors, error => error.PropertyName == nameof(request.DecisionReason));
    }

    private static OfferCreateRequest CreateRequest() => new()
    {
        ServiceRequestId = 1, TradespersonProfileId = 1, Price = 100m,
        ProposedStartUtc = DateTime.UtcNow.AddDays(2), DurationMinutes = 60
    };

    private static OfferUpdateRequest UpdateRequest() => new()
    {
        Id = 1, ServiceRequestId = 1, TradespersonProfileId = 1, Price = 100m,
        ProposedStartUtc = DateTime.UtcNow.AddDays(2), DurationMinutes = 60
    };
}
