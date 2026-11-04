using Majstorio.Model.Requests.City;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Mapping;
using Majstorio.Services.Services;
using Majstorio.Services.Validators.City;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

public class CityServiceTests : IDisposable
{
    private readonly MajstorioDbContext _ctx;
    private readonly CityService _service;

    public CityServiceTests()
    {
        _ctx = new MajstorioDbContext(new DbContextOptionsBuilder<MajstorioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = new TypeAdapterConfig();
        new CityMappingProfile().Register(config);
        _service = new CityService(_ctx, new Mapper(config));
    }

    [Fact]
    public async Task GetCities_ReturnsRequestedPageSortedByNameWithTotal()
    {
        await AddCitiesAsync("Zenica", "Mostar", "Tuzla", "Bihać", "Sarajevo");

        var result = await _service.GetCitiesAsync(new CitySearchRequest { Page = 2, PageSize = 2 });

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.TotalCount);
        Assert.Equal(["Sarajevo", "Tuzla"], result.Value.Items.Select(city => city.Name));
        Assert.Equal(2, result.Value.Page);
        Assert.Equal(2, result.Value.PageSize);
    }

    [Fact]
    public async Task GetCities_FiltersByNameAndActiveState()
    {
        await AddCitiesAsync("Mostar", "Sarajevo");
        _ctx.Cities.Add(new City { Name = "Mostar Jug", IsActive = false });
        await _ctx.SaveChangesAsync();

        var byName = await _service.GetCitiesAsync(new CitySearchRequest { Name = "  Mos " });
        var activeByName = await _service.GetCitiesAsync(new CitySearchRequest { Name = "Mos", IsActive = true });

        Assert.Equal(2, byName.Value!.TotalCount);
        Assert.Equal("Mostar", Assert.Single(activeByName.Value!.Items).Name);
    }

    [Fact]
    public async Task Create_TrimsNameAndReturnsTheNewCity()
    {
        var result = await _service.CreateCityAsync(new CityInsertRequest { Name = "  Travnik  " });

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Id > 0);
        Assert.Equal("Travnik", result.Value.Name);
        Assert.True(result.Value.IsActive);
        Assert.Equal("Travnik", (await _ctx.Cities.SingleAsync()).Name);
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsConflict()
    {
        await AddCitiesAsync("Mostar");

        var result = await _service.CreateCityAsync(new CityInsertRequest { Name = " Mostar " });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Single(await _ctx.Cities.ToListAsync());
    }

    [Fact]
    public async Task Update_ChangesNameAndActiveState()
    {
        var city = (await AddCitiesAsync("Travnik")).Single();

        var result = await _service.UpdateCityAsync(city.Id, new CityUpdateRequest { Name = " Travnik Centar ", IsActive = false });

        Assert.True(result.IsSuccess);
        _ctx.ChangeTracker.Clear();
        var stored = await _ctx.Cities.SingleAsync();
        Assert.Equal("Travnik Centar", stored.Name);
        Assert.False(stored.IsActive);
        Assert.Equal(city.Id, stored.Id);
    }

    [Fact]
    public async Task Update_NameOfAnotherCity_ReturnsConflict()
    {
        var cities = await AddCitiesAsync("Mostar", "Sarajevo");

        var result = await _service.UpdateCityAsync(cities[0].Id, new CityUpdateRequest { Name = "Sarajevo", IsActive = true });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task MissingCity_ReturnsNotFoundForReadUpdateAndDelete()
    {
        Assert.Equal(ErrorType.NotFound, (await _service.GetCityByIdAsync(999)).ErrorType);
        Assert.Equal(ErrorType.NotFound, (await _service.UpdateCityAsync(999, new CityUpdateRequest { Name = "X" })).ErrorType);
        Assert.Equal(ErrorType.NotFound, (await _service.DeleteCityAsync(new CityDeleteRequest { Id = 999 })).ErrorType);
    }

    [Fact]
    public async Task Delete_UnusedCity_RemovesIt()
    {
        var city = (await AddCitiesAsync("Travnik")).Single();

        var result = await _service.DeleteCityAsync(new CityDeleteRequest { Id = city.Id });

        Assert.True(result.IsSuccess);
        Assert.Empty(await _ctx.Cities.ToListAsync());
    }

    [Fact]
    public async Task Delete_CityUsedByAServiceRequest_ReturnsConflictAndKeepsIt()
    {
        var city = (await AddCitiesAsync("Mostar")).Single();
        _ctx.ServiceRequests.Add(new ServiceRequest
        {
            CityId = city.Id,
            Customer = new ApplicationUser { UserName = "ana", PasswordHash = "h", PasswordSalt = "s" },
            ServiceCategory = new ServiceCategory { Name = "Plumbing" },
            Description = "Leaking tap"
        });
        await _ctx.SaveChangesAsync();

        var result = await _service.DeleteCityAsync(new CityDeleteRequest { Id = city.Id });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Contains("Deactivate", result.Message);
        Assert.Single(await _ctx.Cities.ToListAsync());
    }

    [Theory]
    [InlineData(0, 10, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    [InlineData(1, 100, true)]
    [InlineData(3, 1, true)]
    public void SearchValidator_LimitsPageAndPageSize(int page, int pageSize, bool isValid)
    {
        var result = new CitySearchRequestValidator().Validate(new CitySearchRequest { Page = page, PageSize = pageSize });

        Assert.Equal(isValid, result.IsValid);
    }

    [Fact]
    public void InsertValidator_RejectsNamesLongerThanTheColumn()
    {
        var result = new CityInsertRequestValidator().Validate(new CityInsertRequest { Name = new string('a', 101) });

        Assert.Contains(result.Errors, error => error.ErrorMessage == "City name can have at most 100 characters.");
    }

    private async Task<List<City>> AddCitiesAsync(params string[] names)
    {
        var cities = names.Select(name => new City { Name = name }).ToList();
        _ctx.Cities.AddRange(cities);
        await _ctx.SaveChangesAsync();
        return cities;
    }

    public void Dispose() => _ctx.Dispose();
}
