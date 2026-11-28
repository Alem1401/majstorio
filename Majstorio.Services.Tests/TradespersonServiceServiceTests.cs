using Majstorio.Model.Requests.TradespersonService;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Mapping;
using Majstorio.Services.Services;
using Mapster;
using MapsterMapper;
using Xunit;

namespace Majstorio.Services.Tests;

public class TradespersonServiceServiceTests : IDisposable
{
    private readonly MajstorioDbContext _ctx = TestSupport.NewInMemoryContext();
    private readonly Scenario _s;
    private readonly TradespersonServiceService _service;

    public TradespersonServiceServiceTests()
    {
        _s = new Scenario(_ctx);
        var config = new TypeAdapterConfig();
        new TradespersonServiceMappingProfile().Register(config);
        _service = new TradespersonServiceService(_ctx, new Mapper(config));
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task GetServices_ReturnsOnlyTheProfilesServicesSortedByName()
    {
        _s.AddProfile();
        await _service.CreateServiceAsync(NewRequest("Boilers"));

        var page = (await _service.GetServicesAsync(new() { TradespersonProfileId = _s.Profile.Id })).Value!;

        Assert.Equal(["Boilers", "Repairs"], page.Items.Select(service => service.Name));
        Assert.Equal(_s.Category.Name, page.Items[0].ServiceCategoryName);
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task Create_SavesAnActiveTrimmedService()
    {
        var created = (await _service.CreateServiceAsync(NewRequest("  Boilers "))).Value!;

        var saved = (await _service.GetServiceByIdAsync(created.Id)).Value!;
        Assert.Equal("Boilers", saved.Name);
        Assert.Equal(30m, saved.MinimumPrice);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task Create_MissingProfileOrInactiveCategoryOrDuplicateName_IsRejected()
    {
        var noProfile = NewRequest("Boilers");
        noProfile.TradespersonProfileId = 999;
        var inactive = new ServiceCategory { Name = "Inactive", IsActive = false };
        _ctx.Add(inactive);
        await _ctx.SaveChangesAsync();
        var noCategory = NewRequest("Boilers");
        noCategory.ServiceCategoryId = inactive.Id;

        Assert.Equal(ErrorType.NotFound, (await _service.CreateServiceAsync(noProfile)).ErrorType);
        Assert.Equal(ErrorType.NotFound, (await _service.CreateServiceAsync(noCategory)).ErrorType);
        Assert.Equal(ErrorType.Conflict, (await _service.CreateServiceAsync(NewRequest("Repairs"))).ErrorType);
    }

    [Fact]
    public async Task Update_ChangesTheServiceButNotItsProfile()
    {
        var id = _ctx.TradespersonServices.Single().Id;

        var result = await _service.UpdateServiceAsync(new()
        {
            Id = id, ServiceCategoryId = _s.Category.Id, Name = "Pipes", Description = "Pipes",
            MinimumPrice = 10m, MaximumPrice = 50m, EstimatedDurationMinutes = 45, IsActive = false
        });

        Assert.True(result.IsSuccess);
        var service = _ctx.TradespersonServices.Single();
        Assert.Equal("Pipes", service.Name);
        Assert.False(service.IsActive);
        Assert.Equal(_s.Profile.Id, service.TradespersonProfileId);
    }

    [Fact]
    public async Task UpdateAndDelete_MissingService_IsNotFound()
    {
        var update = await _service.UpdateServiceAsync(new() { Id = 999, Name = "Pipes" });
        var delete = await _service.DeleteServiceAsync(new() { Id = 999 });

        Assert.Equal(ErrorType.NotFound, update.ErrorType);
        Assert.Equal(ErrorType.NotFound, delete.ErrorType);
    }

    [Fact]
    public async Task Delete_RemovesTheService()
    {
        var id = _ctx.TradespersonServices.Single().Id;

        Assert.True((await _service.DeleteServiceAsync(new() { Id = id })).IsSuccess);

        Assert.Empty(_ctx.TradespersonServices);
    }

    private TradespersonServiceCreateRequest NewRequest(string name) => new()
    {
        TradespersonProfileId = _s.Profile.Id, ServiceCategoryId = _s.Category.Id, Name = name,
        Description = "Work", MinimumPrice = 30m, MaximumPrice = 90m, EstimatedDurationMinutes = 60
    };
}
