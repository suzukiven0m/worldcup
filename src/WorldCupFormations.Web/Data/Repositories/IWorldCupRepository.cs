using WorldCupFormations.Data.Entities;

namespace WorldCupFormations.Data.Repositories;

public interface IWorldCupRepository
{
    Task<List<WorldCup>> GetAllAsync();
    Task<List<Match>> GetMatchesForYearAsync(int year);
    Task<Match?> GetMatchWithLineupAsync(int matchId);
    Task<List<LineupEntry>> SearchPlayersAsync(string query);
    Task<Player?> GetPlayerProfileAsync(int playerId);
    Task<Team?> GetTeamAsync(int teamId);
    Task<List<Match>> GetTeamMatchesAsync(int teamId);
}
