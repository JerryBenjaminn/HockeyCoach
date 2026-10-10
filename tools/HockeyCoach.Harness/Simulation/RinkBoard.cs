using System.Text;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Harness.Simulation;

/// <summary>
/// ASCII rink of a placement snapshot, home team's view: the home team attacks to the right. Home players are upper case
/// (C, LW, RW, LD, RD, G), away players lower case; the puck carrier is marked with <c>*</c>, a loose puck with
/// <c>o</c>. An empty goal node is drawn as <c>[ ]</c>.
/// </summary>
public static class RinkBoard
{
    private const int CellWidth = 8;

    /// <summary>Renders the board.</summary>
    /// <param name="snapshot">Placements of the event.</param>
    /// <param name="rink">The rink.</param>
    /// <param name="label">Player id → short label (position), upper case for home.</param>
    public static string Render(PlacementSnapshot snapshot, Rink rink, Func<PlayerPlacement, string> label)
    {
        var cells = new Dictionary<int, List<string>>();
        foreach (PlayerPlacement p in snapshot.Players)
        {
            string text = label(p) + (snapshot.PuckCarrierId == p.PlayerId ? "*" : string.Empty);
            if (!cells.TryGetValue(p.NodeId, out List<string>? list))
            {
                list = new List<string>();
                cells.Add(p.NodeId, list);
            }

            list.Add(text);
        }

        if (snapshot.IsPuckLoose)
        {
            if (!cells.TryGetValue(snapshot.PuckNodeId, out List<string>? list))
            {
                list = new List<string>();
                cells.Add(snapshot.PuckNodeId, list);
            }

            list.Add("o");
        }

        var sb = new StringBuilder();
        string border = "    +" + new string('-', rink.Length * CellWidth) + "+";
        sb.AppendLine(Header(rink));
        sb.AppendLine(border);
        for (int y = 0; y < rink.Width; y++)
        {
            sb.Append("y=").Append(y).Append(" |");
            for (int x = 0; x < rink.Length; x++)
            {
                int id = rink.IdOf(x, y);
                string content = cells.TryGetValue(id, out List<string>? list) ? string.Join(",", list) : (rink.IsGoalNode(new GridPoint(x, y)) ? "[ ]" : ".");
                if (content.Length > CellWidth - 1)
                {
                    content = content.Substring(0, CellWidth - 1);
                }

                sb.Append(content.PadRight(CellWidth));
            }

            sb.AppendLine("|");
        }

        sb.Append(border);
        return sb.ToString();
    }

    private static string Header(Rink rink)
    {
        var sb = new StringBuilder("     ");
        for (int x = 0; x < rink.Length; x++)
        {
            string zone = rink.ZoneAtX(x) switch
            {
                RinkZone.Defensive => "D",
                RinkZone.Neutral => "N",
                _ => "O",
            };
            sb.Append(("x" + x + zone).PadRight(CellWidth));
        }

        return sb.ToString().TrimEnd() + "   (home attacks →, zones from home view)";
    }
}
