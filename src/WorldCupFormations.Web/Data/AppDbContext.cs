using Microsoft.EntityFrameworkCore;
using WorldCupFormations.Data.Entities;

namespace WorldCupFormations.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<WorldCup> WorldCups => Set<WorldCup>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<LineupEntry> LineupEntries => Set<LineupEntry>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Match>()
            .HasOne(m => m.HomeTeam)
            .WithMany()
            .HasForeignKey(m => m.HomeTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Match>()
            .HasOne(m => m.AwayTeam)
            .WithMany()
            .HasForeignKey(m => m.AwayTeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
