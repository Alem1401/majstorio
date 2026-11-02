namespace Majstorio.Services.Database;

public class TradespersonProfile : BaseEntity
{
    // Links to ApplicationUser.Id; unique, so one account owns at most one profile.
    public int UserId { get; set; }

    public int CityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsApproved { get; set; }

    public int? ApprovedByUserId { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public ApplicationUser User { get; set; } = null!;
    public City City { get; set; } = null!;
    public ApplicationUser? ApprovedByUser { get; set; }

    public ICollection<TradespersonService> TradespersonServices { get; set; } = new List<TradespersonService>();
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
    public ICollection<ServiceRequest> TargetedServiceRequests { get; set; } = new List<ServiceRequest>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
