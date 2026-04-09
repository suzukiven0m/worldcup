namespace WorldCupFormations.Services;

public enum TeamSide { Home, Away }

public record FormationPosition(string Role, double X, double Y);

public record PlayerOnPitch(string Name, string? ShirtNumber, FormationPosition Position);

public class FormationLayoutService
{
    // SVG viewBox: 0 0 680 1050
    // Home half: Y 30–495 (GK near Y=30, attackers near Y=495)
    // Away half: Y 555–1020 (GK near Y=1020, attackers near Y=555)

    private const double PitchWidth = 680;
    private const double MarginX = 60;
    private const double UsableWidth = PitchWidth - MarginX * 2; // 560

    private static readonly Dictionary<int, string[]> LineRoles = new()
    {
        { 1, ["CF"] },
        { 2, ["RS", "LS"] },
        { 3, ["RW", "CF", "LW"] },
        { 4, ["RB", "CB1", "CB2", "LB"] },
        { 5, ["OR", "IR", "CF", "IL", "OL"] },
        { 6, ["RM", "RCM", "CM1", "CM2", "LCM", "LM"] },
    };

    public IReadOnlyList<FormationPosition> GetPositions(string formation, TeamSide side)
    {
        var lines = formation.Split('-').Select(int.Parse).ToArray();
        var positions = new List<FormationPosition>();

        double gkY   = side == TeamSide.Home ? 65  : 985;
        double defY  = side == TeamSide.Home ? 175 : 875;
        double attY  = side == TeamSide.Home ? 455 : 595;

        // Goalkeeper
        positions.Add(new FormationPosition("GK", PitchWidth / 2.0, gkY));

        int L = lines.Length;
        for (int li = 0; li < L; li++)
        {
            int n = lines[li];
            double y = L == 1
                ? (defY + attY) / 2.0
                : defY + (attY - defY) * li / (double)(L - 1);

            string[] roles = GetRolesForLine(n, li, L);
            double spacing = UsableWidth / (n + 1.0);

            for (int pi = 0; pi < n; pi++)
            {
                double x = MarginX + spacing * (pi + 1);
                positions.Add(new FormationPosition(roles[pi], x, y));
            }
        }

        return positions;
    }

    private static string[] GetRolesForLine(int n, int lineIndex, int totalLines)
    {
        if (LineRoles.TryGetValue(n, out var roles))
            return roles;

        // Fallback for unusual line widths
        return Enumerable.Range(1, n).Select(i => $"P{i}").ToArray();
    }
}
