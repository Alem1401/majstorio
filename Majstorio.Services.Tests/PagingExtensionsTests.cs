using Majstorio.Model.Requests.City;
using Majstorio.Services.Database;
using Majstorio.Services.Extensions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

public class PagingExtensionsTests : IDisposable
{
    private readonly MajstorioDbContext _ctx = new(new DbContextOptionsBuilder<MajstorioDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Theory]
    [InlineData(1, 2, new[] { "A", "B" })]
    [InlineData(2, 2, new[] { "C", "D" })]
    [InlineData(3, 2, new[] { "E" })]
    [InlineData(4, 2, new string[0])]
    public async Task ToPagedAsync_ReturnsTheRequestedSlice(int page, int pageSize, string[] expected)
    {
        await AddCitiesAsync("E", "C", "A", "D", "B");

        var result = await _ctx.Cities.OrderBy(city => city.Name)
            .ToPagedAsync(new CitySearchRequest { Page = page, PageSize = pageSize },
                cities => cities.Select(city => city.Name).ToList());

        Assert.Equal(expected, result.Items);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(page, result.Page);
        Assert.Equal(pageSize, result.PageSize);
    }

    [Fact]
    public async Task ToPagedAsync_CountsOnlyRowsThatPassTheFilter()
    {
        await AddCitiesAsync("A", "B", "C");

        var result = await _ctx.Cities.Where(city => city.Name != "B").OrderBy(city => city.Name)
            .ToPagedAsync(new CitySearchRequest { PageSize = 1 }, cities => cities);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("A", Assert.Single(result.Items).Name);
    }

    private async Task AddCitiesAsync(params string[] names)
    {
        _ctx.Cities.AddRange(names.Select(name => new City { Name = name }));
        await _ctx.SaveChangesAsync();
    }

    public void Dispose() => _ctx.Dispose();
}
