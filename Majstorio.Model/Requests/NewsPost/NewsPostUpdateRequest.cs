namespace Majstorio.Model.Requests.NewsPost;

// The news post id comes from the route (PUT /NewsPost/{id}), not from the body.
public class NewsPostUpdateRequest
{
    public int AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}
