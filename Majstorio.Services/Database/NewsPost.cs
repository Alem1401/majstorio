namespace Majstorio.Services.Database;

public class NewsPost : BaseEntity
{
   
    public int AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;

    public ApplicationUser Author { get; set; } = null!;
}
