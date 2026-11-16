namespace Majstorio.Model.Requests.Offer;

// Changes the terms of a pending offer. The status never comes from the client: it changes only
// through the accept, reject and withdraw actions.
public class OfferUpdateRequest
{
    // Set from the route (PUT /Offers/{id}); a value in the body is overwritten.
    public int Id { get; set; }
    public decimal Price { get; set; }
    public DateTime ProposedStartUtc { get; set; }
    public int DurationMinutes { get; set; }
}
