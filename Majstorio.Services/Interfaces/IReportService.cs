using Majstorio.Model.Requests.Report;
using Majstorio.Model.Responses.Report;
using Majstorio.Model.Results;

public interface IReportService
{
    Task<Result<List<RequestReportRowResponse>>> GetRequestReportAsync(RequestReportRequest request);
    Task<Result<List<TradespersonReportRowResponse>>> GetTradespersonReportAsync(TradespersonReportRequest request);
}
