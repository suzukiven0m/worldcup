namespace WorldCupFormations.Data.Entities;

public class Player
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public string Name { get; set; } = "";
    public string? ShirtNumber { get; set; }
    public string? Club { get; set; }
    public int? Caps { get; set; }
}
