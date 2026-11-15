using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Mapping;
using Majstorio.Services.Services;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

public class ServiceCategoryServiceTests : IDisposable
{
    private readonly MajstorioDbContext _ctx;
    private readonly ServiceCategoryService _service;

    public ServiceCategoryServiceTests()
    {
        _ctx = new MajstorioDbContext(new DbContextOptionsBuilder<MajstorioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = new TypeAdapterConfig();
        new ServiceCategoryMappingProfile().Register(config);
        _service = new ServiceCategoryService(_ctx, new Mapper(config));
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task Crud_CreatesUpdatesListsAndDeletesCategory()
    {
        var created = await _service.CreateServiceCategoryAsync(new() { Name = "  Plumbing " });
        Assert.True(created.IsSuccess);
        Assert.Equal("Plumbing", created.Value!.Name);
        Assert.True(created.Value.IsActive);

        var id = created.Value.Id;
        Assert.True((await _service.UpdateServiceCategoryAsync(id, new() { Name = "Electrical", IsActive = false })).IsSuccess);

        var inactive = (await _service.GetServiceCategoriesAsync(new() { Name = "lect", IsActive = false })).Value!;
        Assert.Equal("Electrical", Assert.Single(inactive.Items).Name);
        Assert.Empty((await _service.GetServiceCategoriesAsync(new() { IsActive = true })).Value!.Items);

        Assert.True((await _service.DeleteServiceCategoryAsync(new() { Id = id })).IsSuccess);
        Assert.Equal(ErrorType.NotFound, (await _service.GetServiceCategoryByIdAsync(id)).ErrorType);
    }

    [Fact]
    public async Task CreateAndUpdate_DuplicateName_ReturnsConflict()
    {
        await _service.CreateServiceCategoryAsync(new() { Name = "Plumbing" });
        var second = (await _service.CreateServiceCategoryAsync(new() { Name = "Electrical" })).Value!;

        Assert.Equal(ErrorType.Conflict, (await _service.CreateServiceCategoryAsync(new() { Name = "Plumbing" })).ErrorType);
        Assert.Equal(ErrorType.Conflict,
            (await _service.UpdateServiceCategoryAsync(second.Id, new() { Name = "Plumbing", IsActive = true })).ErrorType);
        // Saving a category under its own name is allowed.
        Assert.True((await _service.UpdateServiceCategoryAsync(second.Id, new() { Name = "Electrical", IsActive = true })).IsSuccess);
    }

    [Fact]
    public async Task Delete_CategoryInUse_ReturnsConflict()
    {
        var category = new ServiceCategory { Name = "Plumbing" };
        _ctx.ServiceRequests.Add(new ServiceRequest
        {
            ServiceCategory = category, City = new City { Name = "Mostar" },
            Customer = new ApplicationUser
            {
                UserName = "amra", Email = "amra@example.com", FirstName = "Amra", LastName = "Test",
                PasswordHash = "hash", PasswordSalt = "salt"
            },
            Description = "Leaking tap", MaximumBudget = 100m
        });
        await _ctx.SaveChangesAsync();

        var result = await _service.DeleteServiceCategoryAsync(new() { Id = category.Id });

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Single(await _ctx.ServiceCategories.ToListAsync());
    }
}
