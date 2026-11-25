using System.Text.Json.Serialization;

namespace Majstorio.Model.Requests.Review;

public class ReviewUpdateRequest
{
    // Filled in by the controller from the route (PUT /Reviews/{id}).
    // [JsonIgnore]: an id sent in the body is never read, so the client cannot pick another record.
    [JsonIgnore]
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
