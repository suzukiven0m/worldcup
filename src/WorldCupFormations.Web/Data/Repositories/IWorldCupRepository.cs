using WorldCupFormations.Data.Entities;

namespace WorldCupFormations.Data.Repositories;

public interface IWorldCupRepository
{
    Task<List<WorldCup>> GetAllAsync();
    Task<List<Match>> GetMatchesForYearAsync(int year);
    Task<Match?> GetMatchWithLineupAsync(int matchId);
}
