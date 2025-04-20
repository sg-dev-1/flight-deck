using Microsoft.EntityFrameworkCore;

public class FlightDbContext : DbContext
{
    public FlightDbContext(DbContextOptions<FlightDbContext> options)
        : base(options)
    {
    }

    public DbSet<Flight> Flights { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Flight>()
            .HasIndex(f => f.FlightNumber)
            .IsUnique();
    }
}