using System.Text.Json.Serialization;

namespace Majstorio.Model.Requests.Offer;

// Changes the terms of a pending offer. The status never comes from the client: it changes only
// through the accept, reject and withdraw actions.
public class OfferUpdateRequest
{
    // Filled in by the controller from the route (PUT /Offers/{id}).
    // [JsonIgnore]: an id sent in the body is never read, so the client cannot pick another record.
    [JsonIgnore]
    public int Id { get; set; }
    public decimal Price { get; set; }
    public DateTime ProposedStartUtc { get; set; }
    public int DurationMinutes { get; set; }
}
