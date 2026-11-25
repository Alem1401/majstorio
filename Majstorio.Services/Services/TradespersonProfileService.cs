using Majstorio.Model.Requests.TradespersonProfile;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Review;
using Majstorio.Model.Responses.TradespersonProfile;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

public class TradespersonProfileService : ITradespersonProfileService
{
    private const string NotFoundMessage = "Tradesperson profile not found.";
    private const int LatestReviewCount = 5;

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

        // Best rated first; profiles without reviews come last.
        var page = await query
            .OrderByDescending(profile => profile.Reviews.Average(review => (double?)review.Rating))
            .ThenBy(profile => profile.Id)
            .Select(profile => new TradespersonProfileResponse
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
            })
            .ToPagedAsync(search, profiles => profiles);

        // The recommender reads these rows, so only the first page of a mobile user's search is logged.
        if (!user.IsAdministrator && search.Page == 1)
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

    // Unapproved or deactivated profiles are visible only to administrators and to their owner.
    private IQueryable<TradespersonProfile> VisibleTo(CurrentUser user)
    {
        var query = _ctx.TradespersonProfiles.AsNoTracking();
        return user.IsAdministrator
            ? query
            : query.Where(profile => (profile.IsApproved && profile.IsActive) || profile.UserId == user.Id);
    }
}
