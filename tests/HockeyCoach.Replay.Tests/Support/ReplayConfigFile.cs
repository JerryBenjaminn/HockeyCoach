using HockeyCoach.Replay.Core;

namespace HockeyCoach.Replay.Tests.Support;

/// <summary>The real <c>data/replay.json</c> (D-088 B): the tests read the same file as Unity.</summary>
public static class ReplayConfigFile
{
    private static readonly Lazy<string> LazyText = new(() => File.ReadAllText(Path.Combine(Seed42Match.RepoRoot(), "data", "replay.json")));

    /// <summary>The file text.</summary>
    public static string Text => LazyText.Value;

    /// <summary>The file read by <see cref="PresentationConfigReader"/>.</summary>
    public static PresentationConfig Read() => PresentationConfigReader.Read(Text);
}
