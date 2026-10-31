using Majstorio.Model.Requests.NewsPost;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Mapping;
using Majstorio.Services.Services;
using Majstorio.Services.Validators.NewsPost;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

public class NewsPostServiceTests : IDisposable
{
    private readonly MajstorioDbContext _ctx;
    private readonly NewsPostService _service;

    public NewsPostServiceTests()
    {
        _ctx = new MajstorioDbContext(new DbContextOptionsBuilder<MajstorioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = new TypeAdapterConfig();
        new NewsPostMappingProfile().Register(config);
        _service = new NewsPostService(_ctx, new Mapper(config));
    }

    [Fact]
    public async Task Create_MissingAuthor_ReturnsNotFoundWithoutSaving()
    {
        var result = await _service.CreateNewsPostAsync(CreateRequest(999));

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Empty(await _ctx.NewsPosts.ToListAsync());
    }

    [Fact]
    public async Task Create_InactiveAuthor_ReturnsForbiddenWithoutSaving()
    {
        var author = await AddAuthorAsync(isActive: false);

        var result = await _service.CreateNewsPostAsync(CreateRequest(author.Id));

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Empty(await _ctx.NewsPosts.ToListAsync());
    }

    [Theory]
    [InlineData(null, "Content")]
    [InlineData("", "Content")]
    [InlineData(" \t\r\n", "Content")]
    [InlineData("Title", null)]
    [InlineData("Title", "")]
    [InlineData("Title", " \t\r\n")]
    public void BlankTitleOrContent_IsRejectedByValidators(string? title, string? content)
    {
        var create = CreateRequest(1);
        create.Title = title!;
        create.Content = content!;
        var update = new NewsPostUpdateRequest { Id = 1, AuthorId = 1 };
        update.Title = title!;
        update.Content = content!;

        Assert.False(new NewsPostCreateRequestValidator().Validate(create).IsValid);
        Assert.False(new NewsPostUpdateRequestValidator().Validate(update).IsValid);
    }

    [Theory]
    [InlineData("not a URL")]
    [InlineData("/images/news.jpg")]
    [InlineData("ftp://example.com/news.jpg")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/png;base64,AAAA")]
    public void InvalidImageUrl_IsRejectedByValidators(string imageUrl)
    {
        var create = CreateRequest(1);
        create.ImageUrl = imageUrl;
        var update = new NewsPostUpdateRequest { Id = 1, AuthorId = 1, Title = "Title", Content = "Content" };
        update.ImageUrl = imageUrl;

        Assert.False(new NewsPostCreateRequestValidator().Validate(create).IsValid);
        Assert.False(new NewsPostUpdateRequestValidator().Validate(update).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("http://example.com/news.jpg")]
    [InlineData(" https://example.com/news.jpg ")]
    public async Task Create_OptionalOrHttpImageUrl_IsAcceptedAndNormalized(string? imageUrl)
    {
        var author = await AddAuthorAsync();
        var request = CreateRequest(author.Id);
        request.ImageUrl = imageUrl!;

        Assert.True(new NewsPostCreateRequestValidator().Validate(request).IsValid);
        Assert.True((await _service.CreateNewsPostAsync(request)).IsSuccess);
        var stored = await _ctx.NewsPosts.SingleAsync();
        Assert.Equal(imageUrl?.Trim() ?? "", stored.ImageUrl);

        var update = UpdateRequest(stored);
        update.ImageUrl = imageUrl!;
        Assert.True(new NewsPostUpdateRequestValidator().Validate(update).IsValid);
        Assert.True((await _service.UpdateNewsPostAsync(update)).IsSuccess);
        _ctx.ChangeTracker.Clear();
        Assert.Equal(imageUrl?.Trim() ?? "", (await _ctx.NewsPosts.SingleAsync()).ImageUrl);
    }

    [Fact]
    public async Task Crud_NormalizesTextMapsResponsesAndPreservesTimestamps()
    {
        var author = await AddAuthorAsync();
        var request = CreateRequest(author.Id);
        request.Title = "  New title  ";
        request.Content = "  First paragraph\n\nSecond paragraph  ";
        var beforeCreate = DateTime.UtcNow;

        Assert.True((await _service.CreateNewsPostAsync(request)).IsSuccess);
        var post = await _ctx.NewsPosts.SingleAsync();
        var createdAt = post.CreatedAtUtc;
        var publishedAt = post.PublishedAtUtc;
        Assert.InRange(publishedAt, beforeCreate, DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, publishedAt.Kind);
        Assert.Equal("New title", post.Title);
        Assert.Equal("First paragraph\n\nSecond paragraph", post.Content);

        var update = UpdateRequest(post);
        update.Title = "  Edited title  ";
        update.Content = "  Edited content  ";
        update.ImageUrl = "  https://example.com/edited.jpg  ";
        Assert.True((await _service.UpdateNewsPostAsync(update)).IsSuccess);
        _ctx.ChangeTracker.Clear();

        var response = await _service.GetNewsPostByIdAsync(post.Id);
        Assert.True(response.IsSuccess);
        Assert.Equal(post.Id, response.Value!.Id);
        Assert.Equal(author.Id, response.Value.AuthorId);
        Assert.Equal("Edited title", response.Value.Title);
        Assert.Equal("Edited content", response.Value.Content);
        Assert.Equal("https://example.com/edited.jpg", response.Value.ImageUrl);
        Assert.Equal(publishedAt, response.Value.PublishedAtUtc);
        Assert.Equal(createdAt, (await _ctx.NewsPosts.SingleAsync()).CreatedAtUtc);
        Assert.Single((await _service.GetAllNewsPostsAsync()).Value!);

        Assert.True((await _service.DeleteNewsPostAsync(new() { Id = post.Id })).IsSuccess);
        Assert.Empty((await _service.GetAllNewsPostsAsync()).Value!);
    }

    [Fact]
    public async Task Update_MissingAuthor_ReturnsNotFoundWithoutChangingPost()
    {
        var post = await AddPostAsync(await AddAuthorAsync());
        var request = UpdateRequest(post);
        request.AuthorId = 999;
        request.Title = "Changed title";

        Assert.Equal(ErrorType.NotFound, (await _service.UpdateNewsPostAsync(request)).ErrorType);
        _ctx.ChangeTracker.Clear();
        var stored = await _ctx.NewsPosts.SingleAsync();
        Assert.Equal(post.AuthorId, stored.AuthorId);
        Assert.Equal("Original title", stored.Title);
    }

    [Fact]
    public async Task Update_InactiveAuthor_ReturnsForbiddenWithoutChangingPost()
    {
        var post = await AddPostAsync(await AddAuthorAsync(isActive: false));
        var request = UpdateRequest(post);
        request.Title = "Changed title";

        Assert.Equal(ErrorType.Forbidden, (await _service.UpdateNewsPostAsync(request)).ErrorType);
        _ctx.ChangeTracker.Clear();
        Assert.Equal("Original title", (await _ctx.NewsPosts.SingleAsync()).Title);
    }

    [Fact]
    public async Task Update_CanAssignAnotherExistingActiveAuthor()
    {
        var post = await AddPostAsync(await AddAuthorAsync());
        var newAuthor = await AddAuthorAsync();
        var request = UpdateRequest(post);
        request.AuthorId = newAuthor.Id;

        Assert.True((await _service.UpdateNewsPostAsync(request)).IsSuccess);
        _ctx.ChangeTracker.Clear();
        Assert.Equal(newAuthor.Id, (await _ctx.NewsPosts.SingleAsync()).AuthorId);
    }

    [Fact]
    public async Task GetAll_ReturnsNewestFirstWithDeterministicOrderForEqualTimestamps()
    {
        var author = await AddAuthorAsync();
        var oldest = await AddPostAsync(author);
        var newer = await AddPostAsync(author);
        var newestWithSameTimestamp = await AddPostAsync(author);
        oldest.PublishedAtUtc = DateTime.UtcNow.AddDays(-2);
        newer.PublishedAtUtc = oldest.PublishedAtUtc.AddDays(1);
        newestWithSameTimestamp.PublishedAtUtc = newer.PublishedAtUtc;
        await _ctx.SaveChangesAsync();

        var result = await _service.GetAllNewsPostsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { newestWithSameTimestamp.Id, newer.Id, oldest.Id },
            result.Value!.Select(post => post.Id));
    }

    [Fact]
    public async Task MissingPost_ReturnsNotFoundForReadUpdateAndDelete()
    {
        Assert.Equal(ErrorType.NotFound, (await _service.GetNewsPostByIdAsync(999)).ErrorType);
        Assert.Equal(ErrorType.NotFound,
            (await _service.UpdateNewsPostAsync(new() { Id = 999 })).ErrorType);
        Assert.Equal(ErrorType.NotFound,
            (await _service.DeleteNewsPostAsync(new() { Id = 999 })).ErrorType);
    }

    [Fact]
    public void Validators_RejectInvalidIds()
    {
        Assert.Contains(new NewsPostCreateRequestValidator().Validate(CreateRequest(0)).Errors,
            error => error.PropertyName == nameof(NewsPostCreateRequest.AuthorId));
        var update = new NewsPostUpdateRequest { Title = "Title", Content = "Content" };
        var errors = new NewsPostUpdateRequestValidator().Validate(update).Errors;
        Assert.Contains(errors, error => error.PropertyName == nameof(update.Id));
        Assert.Contains(errors, error => error.PropertyName == nameof(update.AuthorId));
        Assert.False(new NewsPostDeleteRequestValidator().Validate(new NewsPostDeleteRequest()).IsValid);
    }

    private static NewsPostCreateRequest CreateRequest(int authorId) => new()
    {
        AuthorId = authorId, Title = "New title", Content = "New content"
    };

    private static NewsPostUpdateRequest UpdateRequest(NewsPost post) => new()
    {
        Id = post.Id, AuthorId = post.AuthorId, Title = post.Title,
        Content = post.Content, ImageUrl = post.ImageUrl
    };

    private async Task<ApplicationUser> AddAuthorAsync(bool isActive = true)
    {
        var author = new ApplicationUser
        {
            UserName = Guid.NewGuid().ToString(), IsActive = isActive,
            PasswordHash = "test-hash", PasswordSalt = "test-salt"
        };
        _ctx.ApplicationUsers.Add(author);
        await _ctx.SaveChangesAsync();
        return author;
    }

    private async Task<NewsPost> AddPostAsync(ApplicationUser author)
    {
        var post = new NewsPost
        {
            AuthorId = author.Id, Title = "Original title", Content = "Original content"
        };
        _ctx.NewsPosts.Add(post);
        await _ctx.SaveChangesAsync();
        return post;
    }

    public void Dispose() => _ctx.Dispose();
}
