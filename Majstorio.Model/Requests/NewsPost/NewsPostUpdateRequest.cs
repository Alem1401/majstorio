using System.Text.Json.Serialization;

namespace Majstorio.Model.Requests.NewsPost;

// The author is not part of the request either: an update never changes who wrote the post.
public class NewsPostUpdateRequest
{
    // Filled in by the controller from the route (PUT /NewsPost/{id}).
    // [JsonIgnore]: an id sent in the body is never read, so the client cannot pick another record.
    [JsonIgnore]
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}
