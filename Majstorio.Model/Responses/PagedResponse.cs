namespace Majstorio.Model.Responses;

// One page of a list endpoint plus the total, so the client can draw its pager.
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
