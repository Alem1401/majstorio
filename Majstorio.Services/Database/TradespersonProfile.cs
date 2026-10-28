namespace Majstorio.Services.Database;

public class TradespersonProfile : BaseEntity
{
   
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
}
