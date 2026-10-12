using HockeyCoach.Harness.Data;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Replay.Core;
using HockeyCoach.Sim.Match;

namespace HockeyCoach.Replay.Tests.Support;

/// <summary>
/// The acceptance match of D-087, generated in process like
/// <c>match --home rulebased --away random --seed 42 --json</c> (default systems), read and built once per test run.
/// No JSON fixture is committed (CLAUDE.md rule 5).
/// </summary>
public sealed class Seed42Match
{
    public const ulong Seed = 42;

    public Seed42Match()
    {
        Data = GameData.Load(Path.Combine(RepoRoot(), "data"));
        Options = MatchCommand.Parse(new[] { "match", "--home", "rulebased", "--away", "random", "--seed", "42", "--json" }, out string? error)
            ?? throw new InvalidOperationException(error);
        Setup = MatchCommand.Setup(Data, Options.Seed, Options.Mirror);
        Result = MatchCommand.Play(Data, Setup, Options.HomeCoach, Options.HomeSystem, Options.AwayCoach, Options.AwaySystem, Options.Seed);
        Json = MatchJson.Write(Result, Setup, Options.Seed);
        Match = ReplayJsonReader.Read(Json);
        Timeline = TimelineBuilder.Build(Match);
        Config = ReplayConfigFile.Read();
        Presentation = PresentationTimeline.Build(Timeline, Config);
    }

    public GameData Data { get; }

    public MatchCommand.Options Options { get; }

    public MatchSetup Setup { get; }

    public MatchResult Result { get; }

    /// <summary>The <c>match --json</c> text.</summary>
    public string Json { get; }

    public ReplayMatch Match { get; }

    public ReplayTimeline Timeline { get; }

    /// <summary>The real <c>data/replay.json</c>.</summary>
    public PresentationConfig Config { get; }

    /// <summary>The presentation warp of <see cref="Timeline"/> with <see cref="Config"/> (D-088).</summary>
    public PresentationTimeline Presentation { get; }

    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HockeyCoach.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("HockeyCoach.sln not found above " + AppContext.BaseDirectory);
    }
}

[CollectionDefinition(Name)]
public sealed class Seed42Collection : ICollectionFixture<Seed42Match>
{
    public const string Name = "Seed 42 match";
}
