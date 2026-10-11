using System.Diagnostics;
using System.Security.Cryptography;
using HockeyCoach.Harness.Data;

namespace HockeyCoach.Harness.Batch;

/// <summary>
/// <c>batch</c>: plays every matchup (default <c>all</c>, mirrors included) with <c>--matches</c> per AI pair in parallel,
/// writes batch-report.md and the CSVs to <c>--out</c> (default reports/, git-ignored) and prints the Markdown report.
/// </summary>
public static class BatchCommand
{
    /// <summary>Runs the command; returns the Markdown report.</summary>
    public static string Run(GameData data, BatchOptions options, string dataDirectory, IReadOnlyList<string> args, Action<string>? progress = null)
    {
        IReadOnlyList<MatchupStats> stats = BatchRunner.Run(data, options, progress);
        BatchReportFiles files = BatchReport.Write(data, options, stats, Header(dataDirectory, args));
        Directory.CreateDirectory(options.OutDirectory);
        File.WriteAllText(Path.Combine(options.OutDirectory, "batch-report.md"), files.Markdown);
        File.WriteAllText(Path.Combine(options.OutDirectory, "batch-summary.csv"), files.SummaryCsv);
        File.WriteAllText(Path.Combine(options.OutDirectory, "batch-matches.csv"), files.MatchesCsv);
        File.WriteAllText(Path.Combine(options.OutDirectory, "batch-details.csv"), files.DetailsCsv);
        return files.Markdown;
    }

    /// <summary>Report header: the command, the git commit and the SHA-256 of tuning.json.</summary>
    public static IReadOnlyList<string> Header(string dataDirectory, IReadOnlyList<string> args)
    {
        string tuningPath = Path.Combine(dataDirectory, GameData.TuningFile);
        string tuningHash = File.Exists(tuningPath) ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(tuningPath))).ToLowerInvariant()[..16] : "missing";
        return new[]
        {
            "Command: batch " + string.Join(" ", args.Skip(1)),
            "Commit: " + Commit(dataDirectory),
            "tuning.json SHA-256: " + tuningHash,
        };
    }

    private static string Commit(string directory)
    {
        try
        {
            var start = new ProcessStartInfo("git", "rev-parse --short HEAD")
            {
                WorkingDirectory = Path.GetFullPath(directory),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using Process? process = Process.Start(start);
            if (process == null)
            {
                return "unknown";
            }

            string hash = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            if (process.ExitCode != 0 || hash.Length == 0)
            {
                return "unknown";
            }

            var status = new ProcessStartInfo("git", "status --porcelain --untracked-files=no -- src tools data")
            {
                WorkingDirectory = Path.GetFullPath(directory),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using Process? dirty = Process.Start(status);
            string changes = dirty?.StandardOutput.ReadToEnd().Trim() ?? string.Empty;
            dirty?.WaitForExit();
            return changes.Length > 0 ? hash + " (uncommitted changes in src/tools/data)" : hash;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return "unknown";
        }
    }
}
