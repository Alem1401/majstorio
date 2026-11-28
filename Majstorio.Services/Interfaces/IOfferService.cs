using Majstorio.Model.Requests.Offer;
using Majstorio.Model.Responses;
using Majstorio.Model.Responses.Offer;
using Majstorio.Model.Results;
using Majstorio.Services.Auth;

public interface IOfferService
{
    // Administrative read-only overview of all offers.
    Task<Result<PagedResponse<OfferResponse>>> GetOffersAsync(OfferSearchRequest search);

    // Offers the user takes part in: sent by them as a tradesperson, or received on their own requests.
    Task<Result<PagedResponse<OfferResponse>>> GetMyOffersAsync(CurrentUser user, OfferSearchRequest search);
    Task<Result> CreateOfferAsync(CurrentUser user, OfferCreateRequest request);
    Task<Result> UpdateOfferAsync(CurrentUser user, OfferUpdateRequest request);
    Task<Result> WithdrawOfferAsync(CurrentUser user, int id);

    // Accepts the offer, closes the others, creates the confirmed appointment and writes the history, in one transaction.
    Task<Result> AcceptOfferAsync(CurrentUser user, int id);
}
