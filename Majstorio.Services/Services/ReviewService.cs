using Majstorio.Model.Requests.Review;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Review;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Extensions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

// Customers rate their own completed jobs; administrators only list and delete reviews (moderation).
public class ReviewService : IReviewService
{
    private const string NotFoundMessage = "Review not found.";
    private const string AppointmentNotFoundMessage = "Appointment not found.";
    private const string DuplicateMessage = "A review already exists for this appointment.";

    private readonly MajstorioDbContext _ctx;
    private readonly IMapper _mapper;

    public ReviewService(MajstorioDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public Task<Result<PagedResponse<ReviewResponse>>> GetReviewsAsync(ReviewSearchRequest search)
        => GetPageAsync(ReviewsWithNames(), search);

    public Task<Result<PagedResponse<ReviewResponse>>> GetMyReviewsAsync(CurrentUser user, ReviewSearchRequest search)
        => GetPageAsync(ReviewsWithNames().Where(review => review.CustomerId == user.Id), search);

    public async Task<Result> CreateMyReviewAsync(CurrentUser user, ReviewCreateRequest request)
    {
        var appointment = await _ctx.Appointments
            .Include(appointment => appointment.Offer.ServiceRequest)
            .FirstOrDefaultAsync(appointment => appointment.Id == request.AppointmentId);

        // Another customer's appointment is reported as missing, so its existence is not revealed.
        if (appointment == null || appointment.Offer.ServiceRequest.CustomerId != user.Id)
            return Result.Failure(ErrorType.NotFound, AppointmentNotFoundMessage);

        if (appointment.Status != AppointmentStatus.Completed)
            return Result.Failure(ErrorType.Conflict, "Only completed appointments can be reviewed.");

        if (await _ctx.Reviews.AnyAsync(review => review.AppointmentId == request.AppointmentId))
            return Result.Failure(ErrorType.Conflict, DuplicateMessage);

        var review = _mapper.Map<Review>(request);
        // The reviewed tradesperson and the reviewing customer come from the job, never from the client.
        review.TradespersonProfileId = appointment.Offer.TradespersonProfileId;
        review.CustomerId = appointment.Offer.ServiceRequest.CustomerId;
        // A comment of only spaces is no comment.
        review.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        _ctx.Reviews.Add(review);

        try
        {
            await _ctx.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            // The same job was rated by a second request that passed the check above at the same moment.
            return Result.Failure(ErrorType.Conflict, DuplicateMessage);
        }
        return Result.Success();
    }

    public async Task<Result> DeleteReviewAsync(ReviewDeleteRequest request)
    {
        var review = await _ctx.Reviews.FirstOrDefaultAsync(review => review.Id == request.Id);
        if (review == null)
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);

        _ctx.Reviews.Remove(review);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    private async Task<Result<PagedResponse<ReviewResponse>>> GetPageAsync(IQueryable<Review> query, ReviewSearchRequest search)
    {
        if (search.Rating.HasValue)
            query = query.Where(review => review.Rating == search.Rating.Value);

        if (search.AppointmentId.HasValue)
            query = query.Where(review => review.AppointmentId == search.AppointmentId.Value);

        var page = await query
            .OrderByDescending(review => review.CreatedAtUtc)
            .ThenByDescending(review => review.Id)
            .ToPagedAsync(search, reviews => reviews.Select(ToResponse).ToList());

        return Result<PagedResponse<ReviewResponse>>.Success(page);
    }

    // Loads the customer and the tradesperson with the reviews in one query, so lists can show names instead of ids.
    private IQueryable<Review> ReviewsWithNames() => _ctx.Reviews.AsNoTracking()
        .Include(review => review.Customer)
        .Include(review => review.TradespersonProfile).ThenInclude(profile => profile.User);

    private ReviewResponse ToResponse(Review review)
    {
        var response = _mapper.Map<ReviewResponse>(review);
        response.CustomerName = PersonNames.Full(review.Customer);
        response.TradespersonName = PersonNames.Full(review.TradespersonProfile?.User);
        return response;
    }
}
