using System.Globalization;
using System.Text;
using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;

namespace HockeyCoach.Harness.Simulation;

/// <summary>
/// <c>shift</c>: simulates one shift with the repository data and prints it. Text with an ASCII rink after every event
/// by default; <c>--no-board</c> prints only the event lines; <c>--json</c> prints the event log as JSON.
/// </summary>
public static class ShiftCommand
{
    /// <summary>Default anti-stall cap for one shift (D-043: a caller parameter).</summary>
    public const int DefaultMaxSteps = 5000;

    /// <summary>Options of the command.</summary>
    /// <param name="Seed">Random seed.</param>
    /// <param name="HomeSystem">Home defensive system id.</param>
    /// <param name="AwaySystem">Away defensive system id.</param>
    /// <param name="Board">Print the ASCII rink after each event.</param>
    /// <param name="Json">Print JSON instead of text.</param>
    /// <param name="MaxSteps">Anti-stall cap.</param>
    public sealed record Options(ulong Seed, string HomeSystem, string AwaySystem, bool Board, bool Json, int MaxSteps);

    /// <summary>Runs one shift and returns the printed output.</summary>
    public static string Run(GameData data, Options options)
    {
        ShiftSetup setup = HarnessTeams.Shift(data, options.HomeSystem, options.AwaySystem, options.MaxSteps);
        ShiftResult result = ShiftSimulator.Run(setup, new Pcg32(options.Seed));
        if (options.Json)
        {
            return EventJson.Write(result, options.Seed);
        }

        Dictionary<int, string> names = Names(setup);
        var text = new EventText(id => names.TryGetValue(id, out string? n) ? n : "#" + id);
        var sb = new StringBuilder();
        sb.AppendLine("Shift, seed " + options.Seed.ToString(CultureInfo.InvariantCulture) + ": Home (" + options.HomeSystem + ") vs Away (" + options.AwaySystem + "), plays: "
            + string.Join(", ", data.Plays.Select(p => p.Id)));
        sb.AppendLine("Home players upper case and attack to the right; away lower case. * = puck carrier, o = loose puck, [ ] = goal.");
        sb.AppendLine();
        foreach (SimEvent e in result.Log.Events)
        {
            sb.AppendLine(text.Line(e));
            if (options.Board)
            {
                sb.AppendLine(RinkBoard.Render(e.Context.Placement, data.Rink, p => Label(p, setup)));
                sb.AppendLine();
            }
        }

        sb.AppendLine("End: " + result.EndReason + " at " + result.EndTime.ToString("0.0", CultureInfo.InvariantCulture) + " s, "
            + result.Log.Count + " events, score Home " + result.HomeGoals + " - " + result.AwayGoals + " Away");
        return sb.ToString();
    }

    /// <summary>Parses <c>shift</c> arguments; returns null and an error message on bad input.</summary>
    public static Options? Parse(string[] args, out string? error)
    {
        error = null;
        ulong seed = 42;
        string home = "forecheck212";
        string away = "trap122";
        bool board = true;
        bool json = false;
        int maxSteps = DefaultMaxSteps;
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seed" when i + 1 < args.Length && ulong.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out ulong s):
                    seed = s;
                    i++;
                    break;
                case "--home-system" when i + 1 < args.Length:
                    home = args[++i];
                    break;
                case "--away-system" when i + 1 < args.Length:
                    away = args[++i];
                    break;
                case "--max-steps" when i + 1 < args.Length && int.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out int m) && m > 0:
                    maxSteps = m;
                    i++;
                    break;
                case "--board":
                    board = true;
                    break;
                case "--no-board":
                    board = false;
                    break;
                case "--json":
                    json = true;
                    break;
                case "--data":
                    i++;
                    break;
                default:
                    error = "Unknown or incomplete option " + args[i];
                    return null;
            }
        }

        return new Options(seed, home, away, board, json, maxSteps);
    }

    private static Dictionary<int, string> Names(ShiftSetup setup)
    {
        var names = new Dictionary<int, string>();
        foreach ((TeamShiftSetup team, string prefix) in new[] { (setup.Home, "H"), (setup.Away, "A") })
        {
            foreach (Skater s in team.Skaters.Skaters)
            {
                names[s.Id] = prefix + "-" + Positions.ToName(team.Skaters.SlotOf(s)!.Value);
            }

            names[team.Goalie.Id] = prefix + "-G";
        }

        return names;
    }

    private static string Label(PlayerPlacement p, ShiftSetup setup)
    {
        TeamShiftSetup team = p.Team == TeamSide.Home ? setup.Home : setup.Away;
        string label = p.IsGoalie ? "G" : Positions.ToName(team.Skaters.SlotOf(team.Skaters.Skaters.First(s => s.Id == p.PlayerId))!.Value);
        return p.Team == TeamSide.Home ? label : label.ToLowerInvariant();
    }
}
