namespace Majstorio.Model.Requests.NewsPost;

public class NewsPostSearchRequest : BaseSearchRequest
{
    // Matches news posts whose title contains this text.
    public string? Title { get; set; }
    public int? AuthorId { get; set; }
}
