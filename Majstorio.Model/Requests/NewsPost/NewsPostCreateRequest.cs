namespace Majstorio.Model.Requests.NewsPost;

// The author is not part of the request: the server uses the signed-in administrator.
public class NewsPostCreateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}
