using Majstorio.Model.Requests.Offer;
using Majstorio.Model.Responses.Offer;
using Majstorio.Model.Results;

public interface IOfferService
{
    Task<Result> CreateOfferAsync(OfferCreateRequest request);
    Task<Result> UpdateOfferAsync(OfferUpdateRequest request);
    Task<Result> DeleteOfferAsync(OfferDeleteRequest request);
    Task<Result<List<OfferResponse>>> GetAllOffersAsync();
    Task<Result<OfferResponse>> GetOfferByIdAsync(int id);
}
