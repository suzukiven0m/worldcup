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

    public async Task<List<LineupEntry>> SearchPlayersAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var q = query.Trim().ToLower();
        await using var db = await factory.CreateDbContextAsync();
        return await db.LineupEntries
            .Include(l => l.Player)
            .Include(l => l.Team)
            .Include(l => l.Match).ThenInclude(m => m.WorldCup)
            .Include(l => l.Match).ThenInclude(m => m.HomeTeam)
            .Include(l => l.Match).ThenInclude(m => m.AwayTeam)
            .Where(l => l.Player.Name.ToLower().Contains(q))
            .OrderBy(l => l.Player.Name)
            .ThenBy(l => l.Match.WorldCup.Year)
            .ToListAsync();
    }

    public async Task<Player?> GetPlayerProfileAsync(int playerId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Players
            .Include(p => p.Team)
            .Include(p => p.LineupEntries)
                .ThenInclude(l => l.Match).ThenInclude(m => m.WorldCup)
            .Include(p => p.LineupEntries)
                .ThenInclude(l => l.Match).ThenInclude(m => m.HomeTeam)
            .Include(p => p.LineupEntries)
                .ThenInclude(l => l.Match).ThenInclude(m => m.AwayTeam)
            .FirstOrDefaultAsync(p => p.Id == playerId);
    }

    public async Task<Team?> GetTeamAsync(int teamId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Teams.FindAsync(teamId);
    }

    public async Task<List<Match>> GetTeamMatchesAsync(int teamId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.WorldCup)
            .Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId)
            .OrderBy(m => m.WorldCup.Year)
            .ThenBy(m => m.Date)
            .ToListAsync();
    }

    public async Task<Match?> FindFinalAsync(int year)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.WorldCup)
            .Where(m => m.WorldCup.Year == year && m.Stage == "Final")
            .FirstOrDefaultAsync();
    }
}
