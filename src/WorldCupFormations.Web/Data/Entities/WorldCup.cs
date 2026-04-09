namespace WorldCupFormations.Data.Entities;

public class WorldCup
{
    public int Id { get; set; }
    public int Year { get; set; }
    public string Host { get; set; } = "";
    public string Winner { get; set; } = "";
    public ICollection<Match> Matches { get; set; } = [];
}
