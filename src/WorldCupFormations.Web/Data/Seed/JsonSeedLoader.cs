using System.Text.Json;
using WorldCupFormations.Data.Entities;
using EntityMatch = WorldCupFormations.Data.Entities.Match;

namespace WorldCupFormations.Data.Seed;

public static class JsonSeedLoader
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static List<WorldCup> LoadWorldCups(string webRootPath)
    {
        var json = ReadJson(webRootPath, "world_cups.json");
        var dtos = JsonSerializer.Deserialize<List<WorldCupDto>>(json, Options)!;
        return dtos.Select(d => new WorldCup
        {
            Id = d.Id, Year = d.Year, Host = d.Host, Winner = d.Winner
        }).ToList();
    }

    public static List<Team> LoadTeams(string webRootPath)
    {
        var json = ReadJson(webRootPath, "teams.json");
        var dtos = JsonSerializer.Deserialize<List<TeamDto>>(json, Options)!;
        return dtos.Select(d => new Team { Id = d.Id, Name = d.Name, Code = d.Code }).ToList();
    }

    public static List<EntityMatch> LoadMatches(string webRootPath)
    {
        var json = ReadJson(webRootPath, "matches.json");
        var dtos = JsonSerializer.Deserialize<List<MatchDto>>(json, Options)!;
        return dtos.Select(d => new EntityMatch
        {
            Id          = d.Id,
            WorldCupId  = d.WorldCupId,
            HomeTeamId  = d.HomeTeamId,
            AwayTeamId  = d.AwayTeamId,
            HomeScore   = d.HomeScore,
            AwayScore   = d.AwayScore,
            Stage       = d.Stage,
            Date        = DateTime.Parse(d.Date)
        }).ToList();
    }

    public static List<Player> LoadPlayers(string webRootPath)
    {
        var json = ReadJson(webRootPath, "players.json");
        var dtos = JsonSerializer.Deserialize<List<PlayerDto>>(json, Options)!;
        return dtos.Select(d => new Player
        {
            Id = d.Id, TeamId = d.TeamId, Name = d.Name, ShirtNumber = d.ShirtNumber
        }).ToList();
    }

    public static List<LineupEntry> LoadLineups(string webRootPath)
    {
        var json = ReadJson(webRootPath, "lineups.json");
        var dtos = JsonSerializer.Deserialize<List<LineupDto>>(json, Options)!;
        return dtos.Select(d => new LineupEntry
        {
            Id           = d.Id,
            MatchId      = d.MatchId,
            TeamId       = d.TeamId,
            PlayerId     = d.PlayerId,
            PositionRole = d.PositionRole,
            Formation    = d.Formation,
            IsStarting   = d.IsStarting
        }).ToList();
    }

    private static string ReadJson(string webRootPath, string fileName)
    {
        var path = Path.Combine(webRootPath, "data", fileName);
        var raw = File.ReadAllText(path);
        // Strip single-line comments (// ...) so we can use annotated JSON
        return System.Text.RegularExpressions.Regex.Replace(raw, @"//[^\n]*", "");
    }

    // ── DTOs ────────────────────────────────────────────────
    private record WorldCupDto(int Id, int Year, string Host, string Winner);
    private record TeamDto(int Id, string Name, string Code);
    private record MatchDto(int Id, int WorldCupId, int HomeTeamId, int AwayTeamId,
        int HomeScore, int AwayScore, string Stage, string Date);
    private record PlayerDto(int Id, int TeamId, string Name, string? ShirtNumber);
    private record LineupDto(int Id, int MatchId, int TeamId, int PlayerId,
        string PositionRole, string Formation, bool IsStarting);
}
