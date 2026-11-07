using Majstorio.Model.Requests;
using Majstorio.Model.Responses;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Extensions;

public static class QueryablePagingExtensions
{
    // Counts the rows and loads one page, so a service only adds its filters and sorting.
    // The query must already be sorted, otherwise the pages are not stable.
    // "map" converts the loaded entities into response objects, for example with the mapper.
    public static async Task<PagedResponse<TResult>> ToPagedAsync<TSource, TResult>(
        this IQueryable<TSource> query,
        BaseSearchRequest search,
        Func<List<TSource>, List<TResult>> map)
    {
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync();

        return new PagedResponse<TResult>
        {
            Items = map(items),
            TotalCount = totalCount,
            Page = search.Page,
            PageSize = search.PageSize
        };
    }
}
