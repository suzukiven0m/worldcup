using Microsoft.EntityFrameworkCore;
using WorldCupFormations.Data.Entities;

namespace WorldCupFormations.Data.Repositories;

public class WorldCupRepository(IDbContextFactory<AppDbContext> factory) : IWorldCupRepository
{
    public async Task<List<WorldCup>> GetAllAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.WorldCups.OrderBy(w => w.Year).ToListAsync();
    }

    public async Task<List<Match>> GetMatchesForYearAsync(int year)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.WorldCup)
            .Where(m => m.WorldCup.Year == year)
            .OrderBy(m => m.Date)
            .ToListAsync();
    }

    public async Task<Match?> GetMatchWithLineupAsync(int matchId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.WorldCup)
            .Include(m => m.LineupEntries)
                .ThenInclude(l => l.Player)
            .Include(m => m.LineupEntries)
                .ThenInclude(l => l.Team)
            .FirstOrDefaultAsync(m => m.Id == matchId);
    }
}
