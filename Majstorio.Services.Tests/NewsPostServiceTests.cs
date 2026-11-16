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
        var result = await _service.CreateNewsPostAsync(999, CreateRequest());

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Empty(await _ctx.NewsPosts.ToListAsync());
    }

    [Fact]
    public async Task Create_InactiveAuthor_ReturnsForbiddenWithoutSaving()
    {
        var author = await AddAuthorAsync(isActive: false);

        var result = await _service.CreateNewsPostAsync(author.Id, CreateRequest());

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
        var create = CreateRequest();
        create.Title = title!;
        create.Content = content!;
        var update = new NewsPostUpdateRequest();
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
        var create = CreateRequest();
        create.ImageUrl = imageUrl;
        var update = new NewsPostUpdateRequest { Title = "Title", Content = "Content" };
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
        var request = CreateRequest();
        request.ImageUrl = imageUrl!;

        Assert.True(new NewsPostCreateRequestValidator().Validate(request).IsValid);
        Assert.True((await _service.CreateNewsPostAsync(author.Id, request)).IsSuccess);
        var stored = await _ctx.NewsPosts.SingleAsync();
        Assert.Equal(imageUrl?.Trim() ?? "", stored.ImageUrl);

        var update = UpdateRequest(stored);
        update.ImageUrl = imageUrl!;
        Assert.True(new NewsPostUpdateRequestValidator().Validate(update).IsValid);
        Assert.True((await _service.UpdateNewsPostAsync(stored.Id, update)).IsSuccess);
        _ctx.ChangeTracker.Clear();
        Assert.Equal(imageUrl?.Trim() ?? "", (await _ctx.NewsPosts.SingleAsync()).ImageUrl);
    }

    [Fact]
    public async Task Crud_NormalizesTextMapsResponsesAndPreservesTimestamps()
    {
        var author = await AddAuthorAsync();
        var request = CreateRequest();
        request.Title = "  New title  ";
        request.Content = "  First paragraph\n\nSecond paragraph  ";
        var beforeCreate = DateTime.UtcNow;

        Assert.True((await _service.CreateNewsPostAsync(author.Id, request)).IsSuccess);
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
        Assert.True((await _service.UpdateNewsPostAsync(post.Id, update)).IsSuccess);
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
        Assert.Single((await _service.GetNewsPostsAsync(new NewsPostSearchRequest())).Value!.Items);

        Assert.True((await _service.DeleteNewsPostAsync(new() { Id = post.Id })).IsSuccess);
        Assert.Empty((await _service.GetNewsPostsAsync(new NewsPostSearchRequest())).Value!.Items);
    }

    [Fact]
    public async Task Update_KeepsTheOriginalAuthor()
    {
        var author = await AddAuthorAsync();
        var post = await AddPostAsync(author);
        var request = UpdateRequest(post);
        request.Title = "Changed title";

        Assert.True((await _service.UpdateNewsPostAsync(post.Id, request)).IsSuccess);
        _ctx.ChangeTracker.Clear();
        var stored = await _ctx.NewsPosts.SingleAsync();
        Assert.Equal(author.Id, stored.AuthorId);
        Assert.Equal("Changed title", stored.Title);
    }

    [Fact]
    public void Requests_HaveNoAuthorField()
    {
        // Nothing the client sends can choose the author; it always comes from the token.
        Assert.Null(typeof(NewsPostCreateRequest).GetProperty("AuthorId"));
        Assert.Null(typeof(NewsPostUpdateRequest).GetProperty("AuthorId"));
    }

    [Fact]
    public async Task Search_ReturnsNewestFirstWithDeterministicOrderForEqualTimestamps()
    {
        var author = await AddAuthorAsync();
        var oldest = await AddPostAsync(author);
        var newer = await AddPostAsync(author);
        var newestWithSameTimestamp = await AddPostAsync(author);
        oldest.PublishedAtUtc = DateTime.UtcNow.AddDays(-2);
        newer.PublishedAtUtc = oldest.PublishedAtUtc.AddDays(1);
        newestWithSameTimestamp.PublishedAtUtc = newer.PublishedAtUtc;
        await _ctx.SaveChangesAsync();

        var result = await _service.GetNewsPostsAsync(new NewsPostSearchRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { newestWithSameTimestamp.Id, newer.Id, oldest.Id },
            result.Value!.Items.Select(post => post.Id));
    }

    [Fact]
    public async Task Search_FiltersByTitleAndAuthorAndPages()
    {
        var first = await AddAuthorAsync();
        var second = await AddAuthorAsync();
        var kitchen = await AddPostAsync(first);
        kitchen.Title = "Kitchen tips";
        var bathroom = await AddPostAsync(first);
        bathroom.Title = "Bathroom tips";
        var other = await AddPostAsync(second);
        other.Title = "Kitchen news";
        await _ctx.SaveChangesAsync();

        var byTitle = await _service.GetNewsPostsAsync(new NewsPostSearchRequest { Title = " Kitchen " });
        var byAuthor = await _service.GetNewsPostsAsync(new NewsPostSearchRequest { AuthorId = first.Id });
        var paged = await _service.GetNewsPostsAsync(new NewsPostSearchRequest { Page = 2, PageSize = 2 });

        Assert.Equal(2, byTitle.Value!.Items.Count);
        Assert.All(byTitle.Value.Items, post => Assert.Contains("Kitchen", post.Title));
        Assert.Equal(2, byAuthor.Value!.Items.Count);
        Assert.Single(paged.Value!.Items);
    }

    [Fact]
    public async Task Create_ReturnsTheCreatedPost()
    {
        var author = await AddAuthorAsync();

        var result = await _service.CreateNewsPostAsync(author.Id, CreateRequest());

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Id > 0);
        Assert.Equal(author.Id, result.Value.AuthorId);
        Assert.Equal("New title", result.Value.Title);
    }

    [Fact]
    public async Task MissingPost_ReturnsNotFoundForReadUpdateAndDelete()
    {
        Assert.Equal(ErrorType.NotFound, (await _service.GetNewsPostByIdAsync(999)).ErrorType);
        Assert.Equal(ErrorType.NotFound,
            (await _service.UpdateNewsPostAsync(999, new())).ErrorType);
        Assert.Equal(ErrorType.NotFound,
            (await _service.DeleteNewsPostAsync(new() { Id = 999 })).ErrorType);
    }

    [Fact]
    public void Validators_RejectInvalidIds()
    {
        Assert.False(new NewsPostDeleteRequestValidator().Validate(new NewsPostDeleteRequest()).IsValid);
    }

    private static NewsPostCreateRequest CreateRequest() => new()
    {
        Title = "New title", Content = "New content"
    };

    private static NewsPostUpdateRequest UpdateRequest(NewsPost post) => new()
    {
        Title = post.Title,
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
