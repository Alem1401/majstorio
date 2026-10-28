namespace Majstorio.Services.Database;

// Shared fields for the application's database entities.
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
