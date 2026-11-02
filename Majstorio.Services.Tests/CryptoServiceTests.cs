using Majstorio.Common.Services;
using Xunit;

namespace Majstorio.Services.Tests;

public class CryptoServiceTests
{
    private readonly CryptoService _crypto = new();

    [Fact]
    public void VerifyPassword_AcceptsTheOriginalPassword()
    {
        var salt = _crypto.GenerateSalt();
        var hash = _crypto.GenerateHash("Correct-Horse-1", salt);

        Assert.True(_crypto.VerifyPassword("Correct-Horse-1", hash, salt));
    }

    [Theory]
    [InlineData("correct-horse-1")]
    [InlineData("Correct-Horse-2")]
    [InlineData("")]
    public void VerifyPassword_RejectsAnyOtherPassword(string attempt)
    {
        var salt = _crypto.GenerateSalt();
        var hash = _crypto.GenerateHash("Correct-Horse-1", salt);

        Assert.False(_crypto.VerifyPassword(attempt, hash, salt));
    }

    [Fact]
    public void VerifyPassword_RejectsTheRightPasswordWithAnotherSalt()
    {
        var hash = _crypto.GenerateHash("Correct-Horse-1", _crypto.GenerateSalt());

        Assert.False(_crypto.VerifyPassword("Correct-Horse-1", hash, _crypto.GenerateSalt()));
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForAMalformedStoredHash()
    {
        Assert.False(_crypto.VerifyPassword("Correct-Horse-1", "not base64!", _crypto.GenerateSalt()));
    }

    [Fact]
    public void GenerateSalt_IsRandomAndSixteenBytes()
    {
        var first = _crypto.GenerateSalt();
        var second = _crypto.GenerateSalt();

        Assert.NotEqual(first, second);
        Assert.Equal(16, Convert.FromBase64String(first).Length);
    }

    [Fact]
    public void GenerateHash_IsDeterministicAndThirtyTwoBytes()
    {
        var salt = _crypto.GenerateSalt();

        var hash = _crypto.GenerateHash("Correct-Horse-1", salt);

        Assert.Equal(hash, _crypto.GenerateHash("Correct-Horse-1", salt));
        Assert.Equal(32, Convert.FromBase64String(hash).Length);
    }
}
