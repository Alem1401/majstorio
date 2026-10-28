using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Database;

public class MajstorioDbContext : DbContext
{
    public MajstorioDbContext(DbContextOptions<MajstorioDbContext> options)
        : base(options)
    {
    }

    // Add DbSet properties here as you implement your entities' services.
    // Configure the database provider and connection in the WebAPI project later.
}
