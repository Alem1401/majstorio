using Majstorio.Model.Requests;
using Majstorio.Model.Requests.Report;
using Majstorio.Model.Responses.Report;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Services;

// Reads the data of the two administrator reports. The desktop app shows it in a table and turns it into a PDF.
public class ReportService : IReportService
{
    private readonly MajstorioDbContext _ctx;

    public ReportService(MajstorioDbContext ctx)
    {
        _ctx = ctx;
    }

    // Report 1: number of service requests per city, category and status.
    public async Task<Result<List<RequestReportRowResponse>>> GetRequestReportAsync(RequestReportRequest request)
    {
        var query = _ctx.ServiceRequests.AsNoTracking();

        if (request.FromUtc.HasValue)
            query = query.Where(serviceRequest => serviceRequest.CreatedAtUtc >= request.FromUtc.Value);

        if (request.ToUtc.HasValue)
            query = query.Where(serviceRequest => serviceRequest.CreatedAtUtc < request.ToUtc.Value);

        if (request.CityId.HasValue)
            query = query.Where(serviceRequest => serviceRequest.CityId == request.CityId.Value);

        if (request.ServiceCategoryId.HasValue)
            query = query.Where(serviceRequest => serviceRequest.ServiceCategoryId == request.ServiceCategoryId.Value);

        if (request.Status.HasValue)
        {
            var status = (RequestStatus)request.Status.Value;
            query = query.Where(serviceRequest => serviceRequest.Status == status);
        }

        // One GROUP BY query; the database does the counting.
        var rows = await query
            .GroupBy(serviceRequest => new
            {
                CityName = serviceRequest.City.Name,
                ServiceCategoryName = serviceRequest.ServiceCategory.Name,
                serviceRequest.Status
            })
            .Select(group => new RequestReportRowResponse
            {
                CityName = group.Key.CityName,
                ServiceCategoryName = group.Key.ServiceCategoryName,
                Status = (int)group.Key.Status,
                RequestCount = group.Count()
            })
            .OrderBy(row => row.CityName).ThenBy(row => row.ServiceCategoryName).ThenBy(row => row.Status)
            .ToListAsync();

        return Result<List<RequestReportRowResponse>>.Success(rows);
    }

    // Report 2: offers, completed jobs and average rating of every approved tradesperson.
    public async Task<Result<List<TradespersonReportRowResponse>>> GetTradespersonReportAsync(
        TradespersonReportRequest request)
    {
        var fromUtc = request.FromUtc;
        var toUtc = request.ToUtc;
        var query = _ctx.TradespersonProfiles.AsNoTracking().Where(profile => profile.IsApproved);

        if (request.CityId.HasValue)
            query = query.Where(profile => profile.CityId == request.CityId.Value);

        // One query: the counts and the average are calculated by the database for each profile.
        var rows = await query
            .Select(profile => new TradespersonReportRowResponse
            {
                TradespersonName = profile.User.FirstName + " " + profile.User.LastName,
                CityName = profile.City.Name,
                OfferCount = profile.Offers.Count(offer =>
                    (fromUtc == null || offer.CreatedAtUtc >= fromUtc)
                    && (toUtc == null || offer.CreatedAtUtc < toUtc)),
                CompletedJobCount = profile.Offers.Count(offer =>
                    offer.Appointment != null
                    && offer.Appointment.Status == AppointmentStatus.Completed
                    && (fromUtc == null || offer.Appointment.EndUtc >= fromUtc)
                    && (toUtc == null || offer.Appointment.EndUtc < toUtc)),
                AverageRating = profile.Reviews
                    .Where(review => (fromUtc == null || review.CreatedAtUtc >= fromUtc)
                        && (toUtc == null || review.CreatedAtUtc < toUtc))
                    .Average(review => (double?)review.Rating)
            })
            .OrderByDescending(row => row.CompletedJobCount).ThenBy(row => row.TradespersonName)
            // A report is one page; the limit keeps a single call from loading the whole table.
            .Take(BaseSearchRequest.MaxPageSize)
            .ToListAsync();

        return Result<List<TradespersonReportRowResponse>>.Success(rows);
    }
}
