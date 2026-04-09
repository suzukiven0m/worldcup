namespace WorldCupFormations.Data.Seed;

public static class SeedData
{
    public static async Task RunAsync(AppDbContext db, string webRootPath)
    {
        var worldCups = JsonSeedLoader.LoadWorldCups(webRootPath);
        var teams     = JsonSeedLoader.LoadTeams(webRootPath);
        var matches   = JsonSeedLoader.LoadMatches(webRootPath);
        var players   = JsonSeedLoader.LoadPlayers(webRootPath);
        var lineups   = JsonSeedLoader.LoadLineups(webRootPath);

        await db.WorldCups.AddRangeAsync(worldCups);
        await db.Teams.AddRangeAsync(teams);
        await db.SaveChangesAsync();

        await db.Matches.AddRangeAsync(matches);
        await db.Players.AddRangeAsync(players);
        await db.SaveChangesAsync();

        await db.LineupEntries.AddRangeAsync(lineups);
        await db.SaveChangesAsync();
    }
}
