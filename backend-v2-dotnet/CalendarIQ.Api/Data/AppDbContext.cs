using Microsoft.EntityFrameworkCore;

namespace CalendarIQ.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Entities.User> Users { get; set; }
    public DbSet<Entities.Event> Events { get; set; }
}