namespace WorldCupFormations.Data.Entities;

public class LineupEntry
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public string PositionRole { get; set; } = "";
    public string Formation { get; set; } = "";
    public bool IsStarting { get; set; }
}
