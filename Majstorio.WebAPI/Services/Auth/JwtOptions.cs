using System.ComponentModel.DataAnnotations;

namespace Majstorio.WebAPI.Services.Auth;

// Bound from the Jwt__* entries in .env; nothing here is hardcoded.
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    // HMAC-SHA256 needs a key of at least 256 bits.
    public const int MinimumKeyLength = 32;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Required]
    [MinLength(MinimumKeyLength, ErrorMessage = "Jwt__Key must be at least 32 characters long.")]
    public string Key { get; init; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Jwt__ExpiryMinutes must be between 1 and 1440.")]
    public int ExpiryMinutes { get; init; }
}
