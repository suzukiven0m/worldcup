namespace WorldCupFormations.Data.Entities;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public ICollection<Player> Players { get; set; } = [];
}
