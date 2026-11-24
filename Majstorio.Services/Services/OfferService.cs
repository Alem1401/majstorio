using Majstorio.Model.Messages;
using Majstorio.Model.Requests.Offer;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Offer;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Extensions;
using Majstorio.Services.RequestStateMachine;
using MapsterMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

public class OfferService : IOfferService
{
    private const string NotFoundMessage = "Offer not found.";
    private const string AlreadyDecidedMessage = "This offer or its request was already decided.";

    private readonly MajstorioDbContext _ctx;
    private readonly IMapper _mapper;
    private readonly IMessagePublisher _publisher;

    public OfferService(MajstorioDbContext ctx, IMapper mapper, IMessagePublisher publisher)
    {
        _ctx = ctx;
        _mapper = mapper;
        _publisher = publisher;
    }

    public async Task<Result> CreateOfferAsync(CurrentUser user, OfferCreateRequest request)
    {
        // The tradesperson is the signed-in user; a profile id is never read from the request.
        var profile = await _ctx.TradespersonProfiles.Include(profile => profile.User)
            .FirstOrDefaultAsync(profile => profile.UserId == user.Id);
        if (profile == null)
            return Result.Failure(ErrorType.Forbidden, "Only tradespeople can submit offers.");

        var validation = await ValidateOfferAsync(request.ServiceRequestId, profile, request.Price);
        if (!validation.IsSuccess)
            return validation;

        if (await _ctx.Offers.AnyAsync(offer => offer.ServiceRequestId == request.ServiceRequestId
            && offer.TradespersonProfileId == profile.Id))
            return Result.Failure(ErrorType.Conflict, "You already have an offer for this request.");

        var offer = _mapper.Map<Offer>(request);
        offer.TradespersonProfileId = profile.Id;
        _ctx.Offers.Add(offer);
        try
        {
            await _ctx.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // Two simultaneous submissions: the unique index let only the first one through.
            return Result.Failure(ErrorType.Conflict, "You already have an offer for this request.");
        }

        return Result.Success();
    }

    public async Task<Result> UpdateOfferAsync(CurrentUser user, OfferUpdateRequest request)
    {
        var offer = await _ctx.Offers
            .Include(offer => offer.TradespersonProfile).ThenInclude(profile => profile.User)
            .FirstOrDefaultAsync(offer => offer.Id == request.Id);
        if (offer == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        if (offer.TradespersonProfile.UserId != user.Id)
            return Result.Failure(ErrorType.Forbidden, "Only the tradesperson who sent the offer can change it.");

        if (offer.Status != OfferStatus.Pending)
            return Result.Failure(ErrorType.Conflict, "Only pending offers can be changed.");

        var validation = await ValidateOfferAsync(offer.ServiceRequestId, offer.TradespersonProfile, request.Price);
        if (!validation.IsSuccess)
            return validation;

        _mapper.Map(request, offer);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> WithdrawOfferAsync(CurrentUser user, int id)
    {
        await using var transaction = await _ctx.Database.BeginTransactionAsync();

        var serviceRequestId = await _ctx.Offers.Where(offer => offer.Id == id)
            .Select(offer => (int?)offer.ServiceRequestId).FirstOrDefaultAsync();
        if (serviceRequestId == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        // Waits for a simultaneous acceptance to finish, so an offer accepted a moment ago cannot be withdrawn.
        await StatusTransitions.LockRequestAsync(_ctx, serviceRequestId.Value);

        var offer = await _ctx.Offers.Include(offer => offer.TradespersonProfile)
            .FirstAsync(offer => offer.Id == id);
        if (offer.TradespersonProfile.UserId != user.Id)
            return Result.Failure(ErrorType.Forbidden, "Only the tradesperson who sent the offer can withdraw it.");

        if (offer.Status != OfferStatus.Pending)
            return Result.Failure(ErrorType.Conflict, "Only an offer that has not been accepted or closed can be withdrawn.");

        offer.Status = OfferStatus.Withdrawn;
        offer.DecidedByUserId = user.Id;
        offer.DecidedAtUtc = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        await transaction.CommitAsync();
        return Result.Success();
    }

    public async Task<Result> AcceptOfferAsync(CurrentUser user, int id)
    {
        // Everything below is one transaction: either all of it is saved or none of it.
        await using var transaction = await _ctx.Database.BeginTransactionAsync();

        var ids = await _ctx.Offers.Where(offer => offer.Id == id)
            .Select(offer => new { offer.ServiceRequestId, offer.TradespersonProfileId }).FirstOrDefaultAsync();
        if (ids == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        // Two locks, always taken in this order and held until the transaction ends:
        // the request, so two acceptances of the same request run one after another and the second sees the first;
        // the tradesperson, so two overlapping appointments cannot both pass the check below.
        await StatusTransitions.LockRequestAsync(_ctx, ids.ServiceRequestId);
        if (_ctx.Database.IsSqlServer())
            await _ctx.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT Id FROM TradespersonProfiles WITH (UPDLOCK, ROWLOCK) WHERE Id = {ids.TradespersonProfileId}");

        var offer = await _ctx.Offers
            .Include(offer => offer.ServiceRequest)
            .Include(offer => offer.TradespersonProfile).ThenInclude(profile => profile.User)
            .FirstOrDefaultAsync(offer => offer.Id == id);
        if (offer == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        var serviceRequest = offer.ServiceRequest;
        if (serviceRequest.CustomerId != user.Id)
            return Result.Failure(ErrorType.Forbidden, "Only the customer who created the request can accept an offer.");

        // A repeated acceptance stops here. A simultaneous one waited for the lock above and now reads the new status.
        if (offer.Status != OfferStatus.Pending || serviceRequest.Status != RequestStatus.Created)
            return Result.Failure(ErrorType.Conflict, AlreadyDecidedMessage);

        var profile = offer.TradespersonProfile;
        if (!profile.IsActive || !profile.IsApproved || !profile.User.IsActive)
            return Result.Failure(ErrorType.Conflict, "This tradesperson is no longer available.");

        if (offer.ProposedStartUtc <= DateTime.UtcNow)
            return Result.Failure(ErrorType.Conflict, "The proposed start of this offer has already passed.");

        var endUtc = offer.ProposedStartUtc.AddMinutes(offer.DurationMinutes);
        if (await _ctx.Appointments.AnyAsync(appointment =>
            (appointment.Status == AppointmentStatus.Pending || appointment.Status == AppointmentStatus.Confirmed)
            && appointment.StartUtc < endUtc && appointment.EndUtc > offer.ProposedStartUtc
            && appointment.Offer.TradespersonProfileId == profile.Id))
            return Result.Failure(ErrorType.Conflict, "The tradesperson already has an appointment at that time.");

        var now = DateTime.UtcNow;
        offer.Status = OfferStatus.Accepted;
        offer.DecidedByUserId = user.Id;
        offer.DecidedAtUtc = now;

        var otherOffers = await _ctx.Offers.Where(other => other.ServiceRequestId == offer.ServiceRequestId
            && other.Id != offer.Id && other.Status == OfferStatus.Pending).ToListAsync();
        foreach (var other in otherOffers)
        {
            other.Status = OfferStatus.Closed;
            other.DecisionReason = "Another offer was accepted for this request.";
            other.DecidedByUserId = user.Id;
            other.DecidedAtUtc = now;
        }

        // The appointment is created as Pending and then confirmed; both steps get their own history row.
        var appointment = new Appointment { Offer = offer, StartUtc = offer.ProposedStartUtc, EndUtc = endUtc };
        var accepted = StatusTransitions.Move(_ctx, serviceRequest, RequestStatus.OfferAccepted, user.Id, now,
            appointment, AppointmentStatus.Pending);
        if (!accepted.IsSuccess)
            return accepted;

        var confirmed = StatusTransitions.Move(_ctx, serviceRequest, RequestStatus.AppointmentConfirmed, user.Id, now,
            appointment, AppointmentStatus.Confirmed);
        if (!confirmed.IsSuccess)
            return confirmed;

        try
        {
            await _ctx.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // Second line of defence: one accepted offer per request and one appointment per offer
            // are unique in the database. The transaction rolls back.
            return Result.Failure(ErrorType.Conflict, AlreadyDecidedMessage);
        }

        // Sent only after the commit, so nobody is told about a change that was rolled back.
        // The Worker turns this message into a Notification row for the tradesperson.
        await _publisher.PublishAsync(new NotificationMessage
        {
            UserId = profile.UserId,
            ServiceRequestId = serviceRequest.Id,
            Title = "Offer accepted",
            Message = "Your offer was accepted and the appointment is confirmed."
        });

        return Result.Success();
    }

    public async Task<Result> DeleteOfferAsync(OfferDeleteRequest request)
    {
        var offer = await _ctx.Offers.FirstOrDefaultAsync(offer => offer.Id == request.Id);
        if (offer == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        if (offer.Status != OfferStatus.Pending || await _ctx.Appointments.AnyAsync(appointment => appointment.OfferId == offer.Id))
            return Result.Failure(ErrorType.Conflict, "Only pending offers without an appointment can be deleted.");

        _ctx.Offers.Remove(offer);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public Task<Result<PagedResponse<OfferResponse>>> GetOffersAsync(OfferSearchRequest search)
        => GetPageAsync(OffersWithNames(), search);

    public Task<Result<PagedResponse<OfferResponse>>> GetMyOffersAsync(CurrentUser user, OfferSearchRequest search)
        => GetPageAsync(OffersWithNames().Where(offer => offer.TradespersonProfile.UserId == user.Id
            || offer.ServiceRequest.CustomerId == user.Id), search);

    public async Task<Result<OfferResponse>> GetOfferByIdAsync(int id)
    {
        var offer = await OffersWithNames().FirstOrDefaultAsync(offer => offer.Id == id);
        if (offer == null)
            return Result<OfferResponse>.Failure(ErrorType.NotFound, NotFoundMessage);

        return Result<OfferResponse>.Success(ToResponse(offer));
    }

    private async Task<Result<PagedResponse<OfferResponse>>> GetPageAsync(IQueryable<Offer> query, OfferSearchRequest search)
    {
        if (search.Status.HasValue)
        {
            var status = (OfferStatus)search.Status.Value;
            query = query.Where(offer => offer.Status == status);
        }

        if (search.ServiceRequestId.HasValue)
            query = query.Where(offer => offer.ServiceRequestId == search.ServiceRequestId.Value);

        var page = await query
            .OrderByDescending(offer => offer.CreatedAtUtc)
            .ThenByDescending(offer => offer.Id)
            .ToPagedAsync(search, offers => offers.Select(ToResponse).ToList());

        return Result<PagedResponse<OfferResponse>>.Success(page);
    }

    // Loads the people and the category with the offers in one query, so lists can show names instead of ids.
    private IQueryable<Offer> OffersWithNames() => _ctx.Offers.AsNoTracking()
        .Include(offer => offer.ServiceRequest).ThenInclude(request => request.Customer)
        .Include(offer => offer.ServiceRequest).ThenInclude(request => request.ServiceCategory)
        .Include(offer => offer.TradespersonProfile).ThenInclude(profile => profile.User);

    private OfferResponse ToResponse(Offer offer)
    {
        var response = _mapper.Map<OfferResponse>(offer);
        response.CustomerName = PersonNames.Full(offer.ServiceRequest?.Customer);
        response.TradespersonName = PersonNames.Full(offer.TradespersonProfile?.User);
        response.ServiceCategoryName = offer.ServiceRequest?.ServiceCategory?.Name ?? string.Empty;
        return response;
    }

    // The profile must be loaded with its user.
    private async Task<Result> ValidateOfferAsync(int serviceRequestId, TradespersonProfile profile, decimal price)
    {
        var serviceRequest = await _ctx.ServiceRequests.FirstOrDefaultAsync(request => request.Id == serviceRequestId);
        if (serviceRequest == null)
            return Result.Failure(ErrorType.NotFound, "Service request not found.");

        if (serviceRequest.Status != RequestStatus.Created)
            return Result.Failure(ErrorType.Conflict, "This request is no longer open for offers.");

        if (!profile.IsActive || !profile.IsApproved || !profile.User.IsActive)
            return Result.Failure(ErrorType.Forbidden, "Only active, approved tradespeople can submit offers.");

        if (profile.UserId == serviceRequest.CustomerId)
            return Result.Failure(ErrorType.Forbidden, "A tradesperson cannot submit an offer for their own request.");

        if (serviceRequest.TargetTradespersonProfileId.HasValue
            && serviceRequest.TargetTradespersonProfileId != profile.Id)
            return Result.Failure(ErrorType.Forbidden, "This request is intended for another tradesperson.");

        if (profile.CityId != serviceRequest.CityId
            || !await _ctx.TradespersonServices.AnyAsync(service => service.TradespersonProfileId == profile.Id
                && service.ServiceCategoryId == serviceRequest.ServiceCategoryId && service.IsActive))
            return Result.Failure(ErrorType.Forbidden, "This request is not in your category and city.");

        if (price > serviceRequest.MaximumBudget)
            return Result.Failure(ErrorType.Conflict, "Offer price exceeds the request's maximum budget.");

        return Result.Success();
    }

    // SQL Server error 2601 / 2627: a row would duplicate a unique index or constraint.
    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
