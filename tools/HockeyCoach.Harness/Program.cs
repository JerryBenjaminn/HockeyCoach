using HockeyCoach.Harness.Data;

namespace HockeyCoach.Harness;

/// <summary>
/// Entry point of the headless test harness. Match, batch and compare commands come with later milestones.
/// </summary>
internal static class Program
{
    private const string DefaultDataDirectory = "data";

    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "validate")
        {
            return Validate(OptionValue(args, "--data") ?? DefaultDataDirectory);
        }

        Console.Error.WriteLine("Usage: validate [--data <dir>]   Load and validate rink, tuning, targets, plays and systems.");
        Console.Error.WriteLine("match, batch and compare are not implemented yet.");
        return 1;
    }

    private static int Validate(string dataDirectory)
    {
        try
        {
            GameData data = GameData.Load(dataDirectory);
            Console.WriteLine(
                "OK: rink " + data.Rink.Length + "x" + data.Rink.Width
                + ", " + (data.Tuning.Checks.Count + 1) + " checks, "
                + data.Targets.Metrics.Count + " target metrics, "
                + data.Plays.Count + " plays, " + data.Systems.Count + " systems (" + Path.GetFullPath(dataDirectory) + ")");
            return 0;
        }
        catch (DataLoadException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static string? OptionValue(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
