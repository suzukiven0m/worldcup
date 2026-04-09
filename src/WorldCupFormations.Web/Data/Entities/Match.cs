namespace WorldCupFormations.Data.Entities;

public class Match
{
    public int Id { get; set; }
    public int WorldCupId { get; set; }
    public WorldCup WorldCup { get; set; } = null!;
    public int HomeTeamId { get; set; }
    public Team HomeTeam { get; set; } = null!;
    public int AwayTeamId { get; set; }
    public Team AwayTeam { get; set; } = null!;
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public string Stage { get; set; } = "";
    public DateTime Date { get; set; }
    public ICollection<LineupEntry> LineupEntries { get; set; } = [];
}
