using Majstorio.Model.Requests.Review;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Review;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Extensions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

public class ReviewService : IReviewService
{
    private readonly MajstorioDbContext _ctx;
    private readonly IMapper _mapper;

    public ReviewService(MajstorioDbContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    public async Task<Result> CreateReviewAsync(ReviewCreateRequest request)
    {
        var appointment = await _ctx.Appointments
            .Include(appointment => appointment.Offer)
                .ThenInclude(offer => offer.ServiceRequest)
            .FirstOrDefaultAsync(appointment => appointment.Id == request.AppointmentId);
        if (appointment == null)
            return Result.Failure(ErrorType.NotFound, "Appointment not found.");

        if (appointment.Status != AppointmentStatus.Completed)
            return Result.Failure(ErrorType.Conflict, "Only completed appointments can be reviewed.");

        if (await _ctx.Reviews.AnyAsync(review => review.AppointmentId == request.AppointmentId))
            return Result.Failure(ErrorType.Conflict, "A review already exists for this appointment.");

        var review = _mapper.Map<Review>(request);
        // The reviewed tradesperson and the reviewing customer come from the job, never from the client.
        review.TradespersonProfileId = appointment.Offer.TradespersonProfileId;
        review.CustomerId = appointment.Offer.ServiceRequest.CustomerId;

        await _ctx.Reviews.AddAsync(review);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateReviewAsync(ReviewUpdateRequest request)
    {
        var review = await _ctx.Reviews.FirstOrDefaultAsync(review => review.Id == request.Id);
        if (review == null)
            return Result.Failure(ErrorType.NotFound, "Review not found.");

        if (review.AppointmentId != request.AppointmentId)
            return Result.Failure(ErrorType.Conflict, "A review cannot be moved to another appointment.");

        _mapper.Map(request, review);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteReviewAsync(ReviewDeleteRequest request)
    {
        var review = await _ctx.Reviews.FirstOrDefaultAsync(review => review.Id == request.Id);
        if (review == null)
            return Result.Failure(ErrorType.NotFound, "Review not found.");

        _ctx.Reviews.Remove(review);
        await _ctx.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<PagedResponse<ReviewResponse>>> GetReviewsAsync(ReviewSearchRequest search)
    {
        var query = ReviewsWithNames();

        if (search.Rating.HasValue)
            query = query.Where(review => review.Rating == search.Rating.Value);

        var page = await query
            .OrderByDescending(review => review.CreatedAtUtc)
            .ThenByDescending(review => review.Id)
            .ToPagedAsync(search, reviews => reviews.Select(ToResponse).ToList());

        return Result<PagedResponse<ReviewResponse>>.Success(page);
    }

    public async Task<Result<ReviewResponse>> GetReviewByIdAsync(int id)
    {
        var review = await ReviewsWithNames().FirstOrDefaultAsync(review => review.Id == id);
        if (review == null)
            return Result<ReviewResponse>.Failure(ErrorType.NotFound, "Review not found.");

        return Result<ReviewResponse>.Success(ToResponse(review));
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
