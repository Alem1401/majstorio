using Majstorio.Model.Requests.Offer;
using Majstorio.Model.Responses.Offer;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

public class OfferService : IOfferService
{
    private readonly MajstorioDbContext _ctx;
    private readonly IMapper _mapper;

    public OfferService(MajstorioDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<Result> CreateOfferAsync(OfferCreateRequest request)
    {
        var validation = await ValidateOfferAsync(
            request.ServiceRequestId, request.TradespersonProfileId, request.Price);
        if (!validation.IsSuccess)
            return validation;

        if (await _ctx.Offers.AnyAsync(offer => offer.ServiceRequestId == request.ServiceRequestId
            && offer.TradespersonProfileId == request.TradespersonProfileId))
            return Result.Failure(ErrorType.Conflict, "This tradesperson already has an offer for this request.");

        await _ctx.Offers.AddAsync(_mapper.Map<Offer>(request));
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateOfferAsync(OfferUpdateRequest request)
    {
        var offer = await _ctx.Offers
            .Include(offer => offer.ServiceRequest)
            .Include(offer => offer.TradespersonProfile)
            .FirstOrDefaultAsync(offer => offer.Id == request.Id);
        if (offer == null)
            return Result.Failure(ErrorType.NotFound, "Offer not found.");

        if (offer.ServiceRequestId != request.ServiceRequestId
            || offer.TradespersonProfileId != request.TradespersonProfileId)
            return Result.Failure(ErrorType.Conflict, "An offer cannot be moved to another request or tradesperson.");

        if (offer.Status != OfferStatus.Pending)
            return Result.Failure(ErrorType.Conflict, "Only pending offers can be changed.");

        if (offer.ServiceRequest.Status != RequestStatus.Created)
            return Result.Failure(ErrorType.Conflict, "This request is no longer open for offers.");

        var status = (OfferStatus)request.Status;
        if (status == OfferStatus.Closed)
            return Result.Failure(ErrorType.Conflict, "Offers are closed automatically when another offer is accepted.");

        if (status == OfferStatus.Pending)
        {
            var validation = await ValidateOfferAsync(request.ServiceRequestId, request.TradespersonProfileId, request.Price);
            if (!validation.IsSuccess)
                return validation;

            _mapper.Map(request, offer);
        }
        else
        {
            if (offer.Price != request.Price || offer.ProposedStartUtc != request.ProposedStartUtc
                || offer.DurationMinutes != request.DurationMinutes)
                return Result.Failure(ErrorType.Conflict, "Offer terms cannot change while recording a decision.");

            var actor = await _ctx.ApplicationUsers.FirstOrDefaultAsync(user => user.Id == request.DecidedByUserId);
            if (actor == null)
                return Result.Failure(ErrorType.NotFound, "Decision user not found.");

            if (!actor.IsActive)
                return Result.Failure(ErrorType.Forbidden, "An inactive user cannot decide on an offer.");

            var expectedUserId = status == OfferStatus.Withdrawn
                ? offer.TradespersonProfile.UserId : offer.ServiceRequest.CustomerId;
            if (actor.Id != expectedUserId)
                return Result.Failure(ErrorType.Forbidden, "Only the customer can accept or reject an offer, and only its tradesperson can withdraw it.");

            List<Offer> competingOffers = [];
            if (status == OfferStatus.Accepted)
            {
                var validation = await ValidateOfferAsync(offer.ServiceRequestId, offer.TradespersonProfileId, offer.Price);
                if (!validation.IsSuccess)
                    return validation;

                var endUtc = offer.ProposedStartUtc.AddMinutes(offer.DurationMinutes);
                if (await _ctx.Appointments.AnyAsync(appointment =>
                    (appointment.Status == AppointmentStatus.Pending || appointment.Status == AppointmentStatus.Confirmed)
                    && appointment.StartUtc < endUtc && appointment.EndUtc > offer.ProposedStartUtc
                    && (appointment.Offer.TradespersonProfileId == offer.TradespersonProfileId
                        || appointment.Offer.ServiceRequest.CustomerId == offer.ServiceRequest.CustomerId)))
                    return Result.Failure(ErrorType.Conflict, "The customer or tradesperson already has an overlapping appointment.");

                competingOffers = await _ctx.Offers.Where(other => other.ServiceRequestId == offer.ServiceRequestId
                    && other.Id != offer.Id && other.Status == OfferStatus.Pending).ToListAsync();
            }

            var decidedAtUtc = DateTime.UtcNow;
            offer.Status = status;
            offer.DecisionReason = request.DecisionReason?.Trim();
            offer.DecidedByUserId = actor.Id;
            offer.DecidedAtUtc = decidedAtUtc;

            if (status == OfferStatus.Accepted)
            {
                foreach (var competingOffer in competingOffers)
                {
                    competingOffer.Status = OfferStatus.Closed;
                    competingOffer.DecisionReason = "Another offer was accepted for this request.";
                    competingOffer.DecidedByUserId = actor.Id;
                    competingOffer.DecidedAtUtc = decidedAtUtc;
                }

                _ctx.StatusHistories.Add(new StatusHistory
                {
                    ServiceRequestId = offer.ServiceRequestId,
                    ChangedByUserId = actor.Id,
                    PreviousStatus = offer.ServiceRequest.Status,
                    NewStatus = RequestStatus.OfferAccepted,
                    ChangedAtUtc = decidedAtUtc,
                    Note = offer.DecisionReason
                });
                offer.ServiceRequest.Status = RequestStatus.OfferAccepted;
            }
        }

        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteOfferAsync(OfferDeleteRequest request)
    {
        var offer = await _ctx.Offers.FirstOrDefaultAsync(offer => offer.Id == request.Id);
        if (offer == null)
            return Result.Failure(ErrorType.NotFound, "Offer not found.");

        if (offer.Status != OfferStatus.Pending || await _ctx.Appointments.AnyAsync(appointment => appointment.OfferId == offer.Id))
            return Result.Failure(ErrorType.Conflict, "Only pending offers without an appointment can be deleted.");

        _ctx.Offers.Remove(offer);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<OfferResponse>>> GetAllOffersAsync()
    {
        var offers = await _ctx.Offers.ToListAsync();
        return Result<List<OfferResponse>>.Success(_mapper.Map<List<OfferResponse>>(offers));
    }

    public async Task<Result<OfferResponse>> GetOfferByIdAsync(int id)
    {
        var offer = await _ctx.Offers.FirstOrDefaultAsync(offer => offer.Id == id);
        if (offer == null)
            return Result<OfferResponse>.Failure(ErrorType.NotFound, "Offer not found.");

        return Result<OfferResponse>.Success(_mapper.Map<OfferResponse>(offer));
    }

    private async Task<Result> ValidateOfferAsync(int serviceRequestId, int tradespersonProfileId, decimal price)
    {
        var serviceRequest = await _ctx.ServiceRequests.FirstOrDefaultAsync(request => request.Id == serviceRequestId);
        if (serviceRequest == null)
            return Result.Failure(ErrorType.NotFound, "Service request not found.");

        var profile = await _ctx.TradespersonProfiles.Include(profile => profile.User)
            .FirstOrDefaultAsync(profile => profile.Id == tradespersonProfileId);
        if (profile == null)
            return Result.Failure(ErrorType.NotFound, "Tradesperson profile not found.");

        if (serviceRequest.Status != RequestStatus.Created
            || await _ctx.Offers.AnyAsync(offer => offer.ServiceRequestId == serviceRequestId && offer.Status == OfferStatus.Accepted))
            return Result.Failure(ErrorType.Conflict, "This request is no longer open for offers.");

        if (!profile.IsActive || !profile.IsApproved || !profile.User.IsActive)
            return Result.Failure(ErrorType.Forbidden, "Only active, approved tradespeople can submit offers.");

        if (profile.UserId == serviceRequest.CustomerId)
            return Result.Failure(ErrorType.Forbidden, "A tradesperson cannot submit an offer for their own request.");

        if (serviceRequest.TargetTradespersonProfileId.HasValue
            && serviceRequest.TargetTradespersonProfileId != tradespersonProfileId)
            return Result.Failure(ErrorType.Forbidden, "This request is intended for another tradesperson.");

        if (profile.CityId != serviceRequest.CityId
            || !await _ctx.TradespersonServices.AnyAsync(service => service.TradespersonProfileId == tradespersonProfileId
                && service.ServiceCategoryId == serviceRequest.ServiceCategoryId && service.IsActive))
            return Result.Failure(ErrorType.Conflict, "The tradesperson must offer an active service in the request's category and city.");

        if (price > serviceRequest.MaximumBudget)
            return Result.Failure(ErrorType.Conflict, "Offer price exceeds the request's maximum budget.");

        return Result.Success();
    }
}
