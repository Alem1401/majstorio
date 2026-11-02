using Majstorio.Services.Enums;

namespace Majstorio.Services.Database;

// A regular user account. Id and CreatedAtUtc are inherited from BaseEntity.
public class ApplicationUser : BaseEntity
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; }
    public string PasswordSalt { get; set; }
    public string? PhoneNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public UserRole Role { get; set; } = UserRole.Customer;
    public int TokenVersion { get; set; }
    public string? PasswordResetTokenHash { get; set; }
    public DateTime? PasswordResetTokenExpiresAtUtc { get; set; }

    // A user has at most one tradesperson profile; null for customers and administrators.
    public TradespersonProfile? TradespersonProfile { get; set; }

    public ICollection<TradespersonProfile> ApprovedTradespersonProfiles { get; set; } = new List<TradespersonProfile>();
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
    public ICollection<Offer> DecidedOffers { get; set; } = new List<Offer>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<NewsPost> NewsPosts { get; set; } = new List<NewsPost>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<SearchLog> SearchLogs { get; set; } = new List<SearchLog>();
    public ICollection<Refund> RequestedRefunds { get; set; } = new List<Refund>();
    public ICollection<StatusHistory> StatusHistories { get; set; } = new List<StatusHistory>();
}
