namespace Majstorio.Model.Requests.NewsPost;

// The news post id comes from the route (PUT /NewsPost/{id}), not from the body.
// The author is not part of the request either: an update never changes who wrote the post.
public class NewsPostUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}
