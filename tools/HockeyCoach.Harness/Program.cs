using HockeyCoach.Harness.Data;

namespace HockeyCoach.Harness;

/// <summary>
/// Entry point of the headless test harness. The compare command comes later in milestone 4.
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

        if (args.Length > 0 && args[0] == "shift")
        {
            return Shift(args);
        }

        if (args.Length > 0 && args[0] == "match")
        {
            return Match(args);
        }

        if (args.Length > 0 && args[0] == "batch")
        {
            return Batch(args);
        }

        Console.Error.WriteLine("Usage: validate [--data <dir>]   Load and validate rink, tuning, targets, plays and systems.");
        Console.Error.WriteLine("       shift [--seed 42] [--home-system forecheck212] [--away-system trap122] [--no-board] [--json] [--max-steps 5000] [--data <dir>]");
        Console.Error.WriteLine("       match [--seed 42] [--home <coach>] [--away <coach>] [--home-system forecheck212] [--away-system trap122] [--json] [--count N] [--data <dir>]");
        Console.Error.WriteLine("             coaches: " + string.Join(", ", Simulation.CoachRegistry.Names) + " (default rotation); --home-system / --away-system apply to rotation only.");
        Console.Error.WriteLine("             --count N (even) plays N/2 pairs: pair i uses seed+i twice, sides swapped; prints averages, venue and team split.");
        Console.Error.WriteLine("       batch [--matches 1000] [--matchups all|a-b,c-d] [--seed 1] [--threads N] [--out reports] [--rotation-system forecheck212] [--mirror] [--data <dir>]");
        Console.Error.WriteLine("             --matches per AI pair (even); all = every pair of the six AIs, mirrors included; writes batch-report.md and CSVs to --out.");
        Console.Error.WriteLine("compare is not implemented yet.");
        return 1;
    }

    private static int Shift(string[] args)
    {
        Simulation.ShiftCommand.Options? options = Simulation.ShiftCommand.Parse(args, out string? error);
        if (options == null)
        {
            Console.Error.WriteLine(error);
            return 1;
        }

        try
        {
            GameData data = GameData.Load(OptionValue(args, "--data") ?? DefaultDataDirectory);
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Write(Simulation.ShiftCommand.Run(data, options));
            return 0;
        }
        catch (DataLoadException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int Match(string[] args)
    {
        Simulation.MatchCommand.Options? options = Simulation.MatchCommand.Parse(args, out string? error);
        if (options == null)
        {
            Console.Error.WriteLine(error);
            return 1;
        }

        try
        {
            GameData data = GameData.Load(OptionValue(args, "--data") ?? DefaultDataDirectory);
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Write(Simulation.MatchCommand.Run(data, options));
            return 0;
        }
        catch (DataLoadException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int Batch(string[] args)
    {
        Batch.BatchOptions? options = HockeyCoach.Harness.Batch.BatchOptions.Parse(args, out string? error);
        if (options == null)
        {
            Console.Error.WriteLine(error);
            return 1;
        }

        try
        {
            string dataDirectory = OptionValue(args, "--data") ?? DefaultDataDirectory;
            GameData data = GameData.Load(dataDirectory);
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            string report = HockeyCoach.Harness.Batch.BatchCommand.Run(data, options, dataDirectory, args, line => Console.Error.WriteLine(line + " (" + watch.Elapsed.ToString(@"hh\:mm\:ss", System.Globalization.CultureInfo.InvariantCulture) + ")"));
            Console.Write(report);
            Console.Error.WriteLine("Report written to " + Path.GetFullPath(options.OutDirectory));
            return 0;
        }
        catch (DataLoadException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
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
