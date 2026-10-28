using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Database;

public class MajstorioDbContext : DbContext
{
    public MajstorioDbContext(DbContextOptions<MajstorioDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<NewsPost> NewsPosts => Set<NewsPost>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<SearchLog> SearchLogs => Set<SearchLog>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<StatusHistory> StatusHistories => Set<StatusHistory>();
    public DbSet<TradespersonProfile> TradespersonProfiles => Set<TradespersonProfile>();
    public DbSet<TradespersonService> TradespersonServices => Set<TradespersonService>();
}
