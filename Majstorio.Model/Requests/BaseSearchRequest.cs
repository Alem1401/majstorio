namespace Majstorio.Model.Requests;

// Paging shared by every list endpoint; PageSize is capped so no call can load a whole table.
public abstract class BaseSearchRequest
{
    public const int MaxPageSize = 100;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
