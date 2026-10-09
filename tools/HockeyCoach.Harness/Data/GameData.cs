using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Harness.Data;

/// <summary>All validated data the simulation and the report need, loaded from the data directory.</summary>
/// <param name="Rink">rink.json.</param>
/// <param name="Tuning">tuning.json, validated against the rink's xG zones.</param>
/// <param name="Targets">targets.json.</param>
/// <param name="Plays">plays/*.json in file-name order, validated against the rink and <c>tuning.plays</c>.</param>
/// <param name="Systems">systems/*.json in file-name order, validated against the rink.</param>
public sealed record GameData(Rink Rink, TuningConfig Tuning, TargetsConfig Targets, IReadOnlyList<Play> Plays, IReadOnlyList<DefensiveSystem> Systems)
{
    /// <summary>File name of the rink data.</summary>
    public const string RinkFile = "rink.json";

    /// <summary>File name of the tuning data.</summary>
    public const string TuningFile = "tuning.json";

    /// <summary>File name of the target ranges.</summary>
    public const string TargetsFile = "targets.json";

    /// <summary>Directory of the play files.</summary>
    public const string PlaysDirectory = "plays";

    /// <summary>Directory of the defensive system files.</summary>
    public const string SystemsDirectory = "systems";

    /// <summary>
    /// Loads and validates rink.json, tuning.json (against the rink), targets.json, plays/*.json and systems/*.json from
    /// <paramref name="dataDirectory"/>. Throws <see cref="DataLoadException"/> listing every error of the first invalid file.
    /// </summary>
    /// <param name="dataDirectory">The data directory, e.g. the repository's <c>data/</c>.</param>
    /// <param name="tuningPath">Optional tuning file overriding <c>dataDirectory/tuning.json</c> (for <c>compare</c>).</param>
    public static GameData Load(string dataDirectory, string? tuningPath = null)
    {
        Rink rink = RinkLoader.Load(Path.Combine(dataDirectory, RinkFile)).GetOrThrow(RinkFile);
        string tuningFile = tuningPath ?? Path.Combine(dataDirectory, TuningFile);
        TuningConfig tuning = TuningLoader.Load(tuningFile, rink.XgZones).GetOrThrow(Path.GetFileName(tuningFile));
        TargetsConfig targets = TargetsLoader.Load(Path.Combine(dataDirectory, TargetsFile)).GetOrThrow(TargetsFile);
        IReadOnlyList<Play> plays = PlayLoader.LoadAll(Path.Combine(dataDirectory, PlaysDirectory), rink, tuning.Plays);
        IReadOnlyList<DefensiveSystem> systems = SystemLoader.LoadAll(Path.Combine(dataDirectory, SystemsDirectory), rink);
        return new GameData(rink, tuning, targets, plays, systems);
    }
}
