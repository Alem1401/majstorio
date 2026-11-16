using Majstorio.Model.Requests.ServiceRequest;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Appointment;
using Majstorio.Model.Responses.Offer;
using Majstorio.Model.Responses.ServiceRequest;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Extensions;
using Majstorio.Services.RequestStateMachine;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

public class ServiceRequestService : IServiceRequestService
{
    private const string NotFoundMessage = "Service request not found.";

    private readonly MajstorioDbContext _ctx;
    private readonly IMapper _mapper;

    public ServiceRequestService(MajstorioDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<Result<PagedResponse<ServiceRequestResponse>>> GetServiceRequestsAsync(
        CurrentUser user, ServiceRequestSearchRequest search)
    {
        var query = VisibleTo(user)
            .Include(request => request.Customer)
            .Include(request => request.City)
            .Include(request => request.ServiceCategory)
            .AsQueryable();

        if (search.Status.HasValue)
        {
            var status = (RequestStatus)search.Status.Value;
            query = query.Where(request => request.Status == status);
        }

        if (search.CityId.HasValue)
            query = query.Where(request => request.CityId == search.CityId.Value);

        if (search.ServiceCategoryId.HasValue)
            query = query.Where(request => request.ServiceCategoryId == search.ServiceCategoryId.Value);

        if (search.FromUtc.HasValue)
            query = query.Where(request => request.CreatedAtUtc >= search.FromUtc.Value);

        if (search.ToUtc.HasValue)
            query = query.Where(request => request.CreatedAtUtc <= search.ToUtc.Value);

        var page = await query
            .OrderByDescending(request => request.CreatedAtUtc)
            .ThenByDescending(request => request.Id)
            .ToPagedAsync(search, requests => requests.Select(ToResponse<ServiceRequestResponse>).ToList());

        return Result<PagedResponse<ServiceRequestResponse>>.Success(page);
    }

    public async Task<Result<ServiceRequestDetailResponse>> GetServiceRequestByIdAsync(CurrentUser user, int id)
    {
        // A request the caller may not see is reported as missing, so ids cannot be probed.
        var request = await VisibleTo(user)
            .Include(request => request.Customer)
            .Include(request => request.City)
            .Include(request => request.ServiceCategory)
            .Include(request => request.Offers).ThenInclude(offer => offer.TradespersonProfile.User)
            .Include(request => request.Offers).ThenInclude(offer => offer.Appointment)
            .Include(request => request.StatusHistories).ThenInclude(history => history.ChangedByUser)
            .AsSplitQuery()
            .FirstOrDefaultAsync(request => request.Id == id);
        if (request == null)
            return Result<ServiceRequestDetailResponse>.Failure(ErrorType.NotFound, NotFoundMessage);

        // The customer and the administrator compare all offers; a tradesperson sees only their own.
        var seesAllOffers = user.IsAdministrator || request.CustomerId == user.Id;
        var offers = request.Offers
            .Where(offer => seesAllOffers || offer.TradespersonProfile.UserId == user.Id)
            .OrderBy(offer => offer.Price)
            .ToList();

        var response = ToResponse<ServiceRequestDetailResponse>(request);
        response.Offers = offers.Select(offer =>
        {
            var offerResponse = _mapper.Map<OfferResponse>(offer);
            offerResponse.CustomerName = response.CustomerName;
            offerResponse.TradespersonName = PersonNames.Full(offer.TradespersonProfile.User);
            offerResponse.ServiceCategoryName = response.ServiceCategoryName;
            return offerResponse;
        }).ToList();

        var acceptedOffer = offers.FirstOrDefault(offer => offer.Appointment != null);
        if (acceptedOffer != null)
        {
            response.Appointment = _mapper.Map<AppointmentResponse>(acceptedOffer.Appointment!);
            response.Appointment.CustomerName = response.CustomerName;
            response.Appointment.TradespersonName = PersonNames.Full(acceptedOffer.TradespersonProfile.User);
            response.Appointment.ServiceCategoryName = response.ServiceCategoryName;
        }

        response.StatusHistory = request.StatusHistories
            .OrderBy(history => history.Id)
            .Select(history =>
            {
                var historyResponse = _mapper.Map<StatusHistoryResponse>(history);
                historyResponse.ChangedByName = PersonNames.Full(history.ChangedByUser);
                return historyResponse;
            }).ToList();

        return Result<ServiceRequestDetailResponse>.Success(response);
    }

    public async Task<Result> CreateServiceRequestAsync(CurrentUser user, ServiceRequestCreateRequest request)
    {
        var validation = await ValidateCategoryAndCityAsync(request.ServiceCategoryId, request.CityId);
        if (!validation.IsSuccess)
            return validation;

        if (request.TargetTradespersonProfileId.HasValue
            && !await _ctx.TradespersonProfiles.AnyAsync(profile => profile.Id == request.TargetTradespersonProfileId
                && profile.IsApproved && profile.IsActive && profile.UserId != user.Id
                && profile.CityId == request.CityId
                && profile.TradespersonServices.Any(service => service.IsActive
                    && service.ServiceCategoryId == request.ServiceCategoryId)))
            return Result.Failure(ErrorType.Conflict,
                "The chosen tradesperson is not available for this category and city.");

        var serviceRequest = _mapper.Map<ServiceRequest>(request);
        // Ownership comes from the token, and every request starts in the same status.
        serviceRequest.CustomerId = user.Id;
        StatusTransitions.RecordCreated(_ctx, serviceRequest, user.Id, DateTime.UtcNow);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateServiceRequestAsync(CurrentUser user, ServiceRequestUpdateRequest request)
    {
        var owned = await FindOwnedEditableAsync(user, request.Id, "changed");
        if (!owned.IsSuccess)
            return owned;

        var validation = await ValidateCategoryAndCityAsync(request.ServiceCategoryId, request.CityId);
        if (!validation.IsSuccess)
            return validation;

        _mapper.Map(request, owned.Value!);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteServiceRequestAsync(CurrentUser user, ServiceRequestDeleteRequest request)
    {
        var owned = await FindOwnedEditableAsync(user, request.Id, "deleted");
        if (!owned.IsSuccess)
            return owned;

        // Only the "created" history row exists at this point, and it goes with the request.
        _ctx.ServiceRequests.Remove(owned.Value!);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> CancelServiceRequestAsync(CurrentUser user, ServiceRequestCancelRequest request)
    {
        // Locked first, so a cancellation and an acceptance of the same request cannot overwrite each other.
        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        await StatusTransitions.LockRequestAsync(_ctx, request.Id);

        var serviceRequest = await _ctx.ServiceRequests
            .Include(serviceRequest => serviceRequest.Offers).ThenInclude(offer => offer.Appointment)
            .Include(serviceRequest => serviceRequest.Offers).ThenInclude(offer => offer.TradespersonProfile)
            .FirstOrDefaultAsync(serviceRequest => serviceRequest.Id == request.Id);
        if (serviceRequest == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        // The customer, the tradesperson whose offer was accepted, or an administrator may cancel.
        var acceptedOffer = serviceRequest.Offers.FirstOrDefault(offer => offer.Status == OfferStatus.Accepted);
        var mayCancel = user.IsAdministrator || serviceRequest.CustomerId == user.Id
            || acceptedOffer?.TradespersonProfile.UserId == user.Id;
        if (!mayCancel)
            return Result.Failure(ErrorType.Forbidden, "You cannot cancel this request.");

        var now = DateTime.UtcNow;
        var appointment = acceptedOffer?.Appointment;
        var move = StatusTransitions.Move(_ctx, serviceRequest, RequestStatus.Cancelled, user.Id, now,
            appointment, appointment == null ? null : AppointmentStatus.Cancelled, request.Reason?.Trim());
        if (!move.IsSuccess)
            return move;

        foreach (var offer in serviceRequest.Offers.Where(offer => offer.Status == OfferStatus.Pending))
        {
            offer.Status = OfferStatus.Closed;
            offer.DecisionReason = "The request was cancelled.";
            offer.DecidedByUserId = user.Id;
            offer.DecidedAtUtc = now;
        }

        await _ctx.SaveChangesAsync();
        await transaction.CommitAsync();
        return Result.Success();
    }

    // Applies the ownership rule to every read: administrators see all requests, customers their own,
    // and a tradesperson additionally sees open requests in their city and category and the ones they made an offer on.
    private IQueryable<ServiceRequest> VisibleTo(CurrentUser user)
    {
        var query = _ctx.ServiceRequests.AsNoTracking();
        if (user.IsAdministrator)
            return query;

        if (user.Role != UserRole.Tradesperson)
            return query.Where(request => request.CustomerId == user.Id);

        return query.Where(request => request.CustomerId == user.Id
            || request.Offers.Any(offer => offer.TradespersonProfile.UserId == user.Id)
            || (request.Status == RequestStatus.Created
                && _ctx.TradespersonProfiles.Any(profile => profile.UserId == user.Id
                    && profile.IsApproved && profile.IsActive
                    && profile.CityId == request.CityId
                    && (request.TargetTradespersonProfileId == null || request.TargetTradespersonProfileId == profile.Id)
                    && profile.TradespersonServices.Any(service => service.IsActive
                        && service.ServiceCategoryId == request.ServiceCategoryId))));
    }

    // A request can be edited or deleted by its customer only while nobody has made an offer on it.
    private async Task<Result<ServiceRequest>> FindOwnedEditableAsync(CurrentUser user, int id, string action)
    {
        var serviceRequest = await _ctx.ServiceRequests.FirstOrDefaultAsync(request => request.Id == id);
        if (serviceRequest == null)
            return Result<ServiceRequest>.Failure(ErrorType.NotFound, NotFoundMessage);

        if (serviceRequest.CustomerId != user.Id)
            return Result<ServiceRequest>.Failure(ErrorType.Forbidden, "Only the customer who created the request can do this.");

        if (serviceRequest.Status != RequestStatus.Created
            || await _ctx.Offers.AnyAsync(offer => offer.ServiceRequestId == id))
            return Result<ServiceRequest>.Failure(ErrorType.Conflict,
                $"A request that has offers or is no longer open cannot be {action}. Cancel it instead.");

        return Result<ServiceRequest>.Success(serviceRequest);
    }

    private async Task<Result> ValidateCategoryAndCityAsync(int serviceCategoryId, int cityId)
    {
        if (!await _ctx.ServiceCategories.AnyAsync(category => category.Id == serviceCategoryId && category.IsActive))
            return Result.Failure(ErrorType.NotFound, "Service category not found or not active.");

        if (!await _ctx.Cities.AnyAsync(city => city.Id == cityId && city.IsActive))
            return Result.Failure(ErrorType.NotFound, "City not found or not active.");

        return Result.Success();
    }

    private T ToResponse<T>(ServiceRequest request) where T : ServiceRequestResponse
    {
        var response = _mapper.Map<T>(request);
        response.CustomerName = PersonNames.Full(request.Customer);
        response.CityName = request.City?.Name ?? string.Empty;
        response.ServiceCategoryName = request.ServiceCategory?.Name ?? string.Empty;
        return response;
    }
}
