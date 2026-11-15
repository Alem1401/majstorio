using Majstorio.Model.Requests.Offer;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Offer;
using Majstorio.Model.Results;

public interface IOfferService
{
    Task<Result<PagedResponse<OfferResponse>>> GetOffersAsync(OfferSearchRequest search);
    Task<Result<OfferResponse>> GetOfferByIdAsync(int id);
    Task<Result> CreateOfferAsync(OfferCreateRequest request);
    Task<Result> UpdateOfferAsync(OfferUpdateRequest request);
    Task<Result> DeleteOfferAsync(OfferDeleteRequest request);
}
