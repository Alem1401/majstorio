namespace Majstorio.Model.Requests.NewsPost;

public class NewsPostUpdateRequest
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}
