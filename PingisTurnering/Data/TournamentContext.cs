using Microsoft.EntityFrameworkCore;

namespace PingisTurnering.Data;

public class TournamentContext : DbContext
{
    public DbSet<Player> Players { get; set; } = null!;
    public DbSet<Match> Matches { get; set; } = null!;

    public string DbPath { get; }

    public TournamentContext()
    {
        var folder = AppContext.BaseDirectory;
        DbPath = System.IO.Path.Join(folder, "tournament.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={DbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Player>().HasKey(p => p.Id);
        modelBuilder.Entity<Match>().HasKey(m => m.Id);
    }
}
