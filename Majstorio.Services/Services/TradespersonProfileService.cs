using Majstorio.Model.Requests.TradespersonProfile;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Review;
using Majstorio.Model.Responses.TradespersonProfile;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Extensions;
using Majstorio.Services.Recommendations;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

public class TradespersonProfileService : ITradespersonProfileService
{
    private const string NotFoundMessage = "Tradesperson profile not found.";
    private const int LatestReviewCount = 5;
    private const int SearchHistoryDays = 30;
    private const int RepeatedSearchMinutes = 10;
    private const double SearchWeight = 1;
    private const double RequestWeight = 1;
    private const double CompletedRequestWeight = 2;

    private readonly MajstorioDbContext _ctx;

    public TradespersonProfileService(MajstorioDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<Result<PagedResponse<TradespersonProfileResponse>>> GetProfilesAsync(
        CurrentUser user, TradespersonProfileSearchRequest search)
    {
        var query = VisibleTo(user);

        if (user.IsAdministrator)
        {
            if (search.IsApproved.HasValue)
                query = query.Where(profile => profile.IsApproved == search.IsApproved.Value);

            if (search.IsActive.HasValue)
                query = query.Where(profile => profile.IsActive == search.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            var name = search.Name.Trim();
            query = query.Where(profile => profile.User.FirstName.Contains(name) || profile.User.LastName.Contains(name));
        }

        if (search.CityId.HasValue)
            query = query.Where(profile => profile.CityId == search.CityId.Value);

        if (search.ServiceCategoryId.HasValue)
            query = query.Where(profile => profile.TradespersonServices.Any(service =>
                service.IsActive && service.ServiceCategoryId == search.ServiceCategoryId.Value));

        if (search.MinimumRating.HasValue)
        {
            var minimum = (double)search.MinimumRating.Value;
            query = query.Where(profile => profile.Reviews.Average(review => (double?)review.Rating) >= minimum);
        }

        // Busy = a pending or confirmed appointment that overlaps the requested interval.
        if (search.AvailableFromUtc.HasValue && search.AvailableToUtc.HasValue)
        {
            var from = search.AvailableFromUtc.Value;
            var to = search.AvailableToUtc.Value;
            query = query.Where(profile => !profile.Offers.Any(offer => offer.Appointment != null
                && (offer.Appointment.Status == AppointmentStatus.Pending || offer.Appointment.Status == AppointmentStatus.Confirmed)
                && offer.Appointment.StartUtc < to && offer.Appointment.EndUtc > from));
        }

        // Administrators manage profiles, so they get a plain list; everyone else gets recommendations.
        var page = user.IsAdministrator
            ? await ToResponses(query
                    // Best rated first; profiles without reviews come last.
                    .OrderByDescending(profile => profile.Reviews.Average(review => (double?)review.Rating))
                    .ThenBy(profile => profile.Id))
                .ToPagedAsync(search, profiles => profiles)
            : await GetRecommendedPageAsync(user, query, search);

        // The recommender reads these rows, so only the first page of a mobile user's search is logged.
        // A list without any filter (the home screen) says nothing about the user's interests.
        // The row is written after ranking, so a new user's first search is still ranked by popularity.
        if (!user.IsAdministrator && search.Page == 1 && HasFilter(search))
        {
            _ctx.SearchLogs.Add(new SearchLog
            {
                UserId = user.Id,
                CityId = search.CityId,
                ServiceCategoryId = search.ServiceCategoryId,
                Query = string.IsNullOrWhiteSpace(search.Name) ? null : search.Name.Trim(),
                MinimumRating = search.MinimumRating,
                AvailableFromUtc = search.AvailableFromUtc,
                AvailableToUtc = search.AvailableToUtc
            });
            await _ctx.SaveChangesAsync();
        }

        return Result<PagedResponse<TradespersonProfileResponse>>.Success(page);
    }

    public async Task<Result<TradespersonProfileDetailResponse>> GetProfileByIdAsync(CurrentUser user, int id)
    {
        var profile = await VisibleTo(user)
            .Include(profile => profile.User)
            .Include(profile => profile.City)
            .Include(profile => profile.TradespersonServices.Where(service => service.IsActive))
                .ThenInclude(service => service.ServiceCategory)
            .AsSplitQuery()
            .FirstOrDefaultAsync(profile => profile.Id == id);
        if (profile == null)
            return Result<TradespersonProfileDetailResponse>.Failure(ErrorType.NotFound, NotFoundMessage);

        var reviews = _ctx.Reviews.AsNoTracking().Where(review => review.TradespersonProfileId == id);
        var reviewCount = await reviews.CountAsync();
        var latestReviews = await reviews.Include(review => review.Customer)
            .OrderByDescending(review => review.CreatedAtUtc).Take(LatestReviewCount).ToListAsync();
        var tradespersonName = PersonNames.Full(profile.User);

        return Result<TradespersonProfileDetailResponse>.Success(new TradespersonProfileDetailResponse
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FullName = tradespersonName,
            ProfileImageUrl = profile.User.ProfileImageUrl,
            CityId = profile.CityId,
            CityName = profile.City.Name,
            Description = profile.Description,
            IsApproved = profile.IsApproved,
            IsActive = profile.IsActive,
            AverageRating = reviewCount == 0 ? null : await reviews.AverageAsync(review => (double)review.Rating),
            ReviewCount = reviewCount,
            CompletedJobs = await _ctx.Appointments.CountAsync(appointment =>
                appointment.Offer.TradespersonProfileId == id && appointment.Status == AppointmentStatus.Completed),
            MinimumPrice = profile.TradespersonServices.Min(service => (decimal?)service.MinimumPrice),
            MaximumPrice = profile.TradespersonServices.Max(service => (decimal?)service.MaximumPrice),
            Categories = profile.TradespersonServices.Select(service => service.ServiceCategory.Name).Distinct().ToList(),
            Services = profile.TradespersonServices.Select(service => new TradespersonServiceResponse
            {
                Id = service.Id,
                ServiceCategoryId = service.ServiceCategoryId,
                ServiceCategoryName = service.ServiceCategory.Name,
                Name = service.Name,
                Description = service.Description,
                MinimumPrice = service.MinimumPrice,
                MaximumPrice = service.MaximumPrice,
                EstimatedDurationMinutes = service.EstimatedDurationMinutes
            }).ToList(),
            LatestReviews = latestReviews.Select(review => new ReviewResponse
            {
                Id = review.Id,
                AppointmentId = review.AppointmentId,
                TradespersonProfileId = review.TradespersonProfileId,
                CustomerId = review.CustomerId,
                CustomerName = PersonNames.Full(review.Customer),
                TradespersonName = tradespersonName,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAtUtc = review.CreatedAtUtc
            }).ToList()
        });
    }

    public async Task<Result> UpdateProfileAsync(TradespersonProfileUpdateRequest request)
    {
        var profile = await _ctx.TradespersonProfiles.FirstOrDefaultAsync(profile => profile.Id == request.Id);
        if (profile == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        // A deactivated city stays on the profiles that already have it, but cannot be newly chosen.
        if (request.CityId != profile.CityId
            && !await _ctx.Cities.AnyAsync(city => city.Id == request.CityId && city.IsActive))
            return Result.Failure(ErrorType.NotFound, "City not found or not active.");

        profile.CityId = request.CityId;
        profile.Description = request.Description.Trim();
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ApproveProfileAsync(CurrentUser administrator, int id)
    {
        var profile = await _ctx.TradespersonProfiles.Include(profile => profile.User)
            .FirstOrDefaultAsync(profile => profile.Id == id);
        if (profile == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        if (profile.IsApproved)
            return Result.Failure(ErrorType.Conflict, "This profile is already approved.");

        profile.IsApproved = true;
        profile.ApprovedByUserId = administrator.Id;
        profile.ApprovedAtUtc = DateTime.UtcNow;
        // The only way to become a tradesperson. Tokens issued so far still carry the old role,
        // so they are invalidated and the next login returns a token with the new one.
        profile.User.Role = UserRole.Tradesperson;
        profile.User.TokenVersion++;
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> SetProfileActiveAsync(TradespersonProfileActiveRequest request)
    {
        var profile = await _ctx.TradespersonProfiles.FirstOrDefaultAsync(profile => profile.Id == request.Id);
        if (profile == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        // An inactive profile is hidden from search and cannot send offers; its history stays.
        profile.IsActive = request.IsActive;
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    private static bool HasFilter(TradespersonProfileSearchRequest search) =>
        !string.IsNullOrWhiteSpace(search.Name) || search.CityId.HasValue || search.ServiceCategoryId.HasValue
        || search.MinimumRating.HasValue || search.AvailableFromUtc.HasValue;

    // Scores every matching profile in memory, then loads the full data of the requested page only.
    private async Task<PagedResponse<TradespersonProfileResponse>> GetRecommendedPageAsync(
        CurrentUser user, IQueryable<TradespersonProfile> query, TradespersonProfileSearchRequest search)
    {
        var categoryId = search.ServiceCategoryId;
        var candidates = await query
            .Select(profile => new RecommendationCandidate(
                profile.Id,
                profile.CityId,
                profile.TradespersonServices.Where(service => service.IsActive)
                    .Select(service => service.ServiceCategoryId).Distinct().ToList(),
                profile.Reviews.Average(review => (double?)review.Rating),
                // With a category filter, popularity counts only the jobs done in that category.
                profile.Offers.Count(offer => offer.Appointment != null
                    && offer.Appointment.Status == AppointmentStatus.Completed
                    && (categoryId == null || offer.ServiceRequest.ServiceCategoryId == categoryId))))
            .ToListAsync();

        var preferences = await GetPreferencesAsync(user, search);
        var ranked = ContentBasedRecommender.Rank(preferences, candidates)
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToList();

        var ids = ranked.Select(recommendation => recommendation.ProfileId).ToList();
        var responses = await ToResponses(query.Where(profile => ids.Contains(profile.Id)))
            .ToDictionaryAsync(response => response.Id);

        return new PagedResponse<TradespersonProfileResponse>
        {
            Items = ranked.Select(recommendation =>
            {
                var response = responses[recommendation.ProfileId];
                response.RecommendationReasons = recommendation.Reasons;
                return response;
            }).ToList(),
            TotalCount = candidates.Count,
            Page = search.Page,
            PageSize = search.PageSize
        };
    }

    // The user's history: searches of the last 30 days and the requests they have not cancelled.
    private async Task<UserPreferences> GetPreferencesAsync(CurrentUser user, TradespersonProfileSearchRequest search)
    {
        var preferences = new UserPreferences();

        var searchedAfter = DateTime.UtcNow.AddDays(-SearchHistoryDays);
        var searches = await _ctx.SearchLogs.AsNoTracking()
            .Where(log => log.UserId == user.Id && log.SearchedAtUtc >= searchedAfter)
            .ToListAsync();

        // A search must not influence its own ranking, otherwise page 2 would be ordered differently
        // from page 1 (which wrote the log) and a new user would never see the popularity ranking.
        var isFiltered = search.CityId.HasValue || search.ServiceCategoryId.HasValue;
        var repeatedAfter = DateTime.UtcNow.AddMinutes(-RepeatedSearchMinutes);
        foreach (var log in searches)
        {
            var isCurrentSearch = isFiltered && log.SearchedAtUtc >= repeatedAfter
                && log.CityId == search.CityId && log.ServiceCategoryId == search.ServiceCategoryId;
            if (!isCurrentSearch)
                preferences.Add(log.ServiceCategoryId, log.CityId, SearchWeight);
        }

        var requests = await _ctx.ServiceRequests.AsNoTracking()
            .Where(request => request.CustomerId == user.Id && request.Status != RequestStatus.Cancelled)
            .Select(request => new { request.ServiceCategoryId, request.CityId, request.Status })
            .ToListAsync();
        foreach (var request in requests)
            preferences.Add(request.ServiceCategoryId, request.CityId,
                request.Status == RequestStatus.Completed ? CompletedRequestWeight : RequestWeight);

        return preferences;
    }

    private static IQueryable<TradespersonProfileResponse> ToResponses(IQueryable<TradespersonProfile> profiles) =>
        profiles.Select(profile => new TradespersonProfileResponse
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FullName = profile.User.FirstName + " " + profile.User.LastName,
            ProfileImageUrl = profile.User.ProfileImageUrl,
            CityId = profile.CityId,
            CityName = profile.City.Name,
            Description = profile.Description,
            IsApproved = profile.IsApproved,
            IsActive = profile.IsActive,
            AverageRating = profile.Reviews.Average(review => (double?)review.Rating),
            ReviewCount = profile.Reviews.Count(),
            CompletedJobs = profile.Offers.Count(offer => offer.Appointment != null
                && offer.Appointment.Status == AppointmentStatus.Completed),
            MinimumPrice = profile.TradespersonServices.Where(service => service.IsActive)
                .Min(service => (decimal?)service.MinimumPrice),
            MaximumPrice = profile.TradespersonServices.Where(service => service.IsActive)
                .Max(service => (decimal?)service.MaximumPrice),
            Categories = profile.TradespersonServices.Where(service => service.IsActive)
                .Select(service => service.ServiceCategory.Name).Distinct().ToList()
        });

    // Unapproved or deactivated profiles are visible only to administrators and to their owner.
    private IQueryable<TradespersonProfile> VisibleTo(CurrentUser user)
    {
        var query = _ctx.TradespersonProfiles.AsNoTracking();
        return user.IsAdministrator
            ? query
            : query.Where(profile => (profile.IsApproved && profile.IsActive) || profile.UserId == user.Id);
    }
}
