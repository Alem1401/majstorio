namespace Majstorio.Model.Responses.NewsPost;

public class NewsPostResponse
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime PublishedAtUtc { get; set; }
}
