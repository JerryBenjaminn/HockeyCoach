using HockeyCoach.AI;
using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Random;

namespace HockeyCoach.Harness.Simulation;

/// <summary>
/// Coach AIs by name for <c>match --home X --away Y</c> (data-schema.md AI-2). <c>rotation</c> plays the system given on
/// the command line; the others choose their own system. <c>random</c>, <c>randomTactics</c> and <c>randomLines</c> draw
/// from their own stream; <c>rulebased</c> and <c>adaptive</c> are deterministic.
/// </summary>
public static class CoachRegistry
{
    /// <summary>The rotation coach (O-4).</summary>
    public const string Rotation = "rotation";

    /// <summary>The random coach (AI-2: R-L and R-T).</summary>
    public const string Random = "random";

    /// <summary>Rotation lines and random tactics (AI-2).</summary>
    public const string RandomTactics = "randomTactics";

    /// <summary>Random lines and rule-based tactics (AI-2).</summary>
    public const string RandomLines = "randomLines";

    /// <summary>Rule-based lines and tactics (AI-2: S-L and S-T).</summary>
    public const string RuleBased = "rulebased";

    /// <summary>Rule-based lines and adaptive tactics (AI-2: S-L and A-T).</summary>
    public const string Adaptive = "adaptive";

    /// <summary>Known names in ordinal order.</summary>
    public static readonly IReadOnlyList<string> Names = new[] { Adaptive, Random, RandomLines, RandomTactics, Rotation, RuleBased }
        .OrderBy(n => n, StringComparer.Ordinal).ToArray();

    /// <summary>True when <paramref name="name"/> uses the command-line defensive system (only <c>rotation</c>).</summary>
    public static bool UsesSystemOption(string name) => name == Rotation;

    /// <summary>Throws <see cref="ArgumentException"/> listing the known names if <paramref name="name"/> is unknown.</summary>
    public static void Require(string name)
    {
        if (!Names.Contains(name))
        {
            throw new ArgumentException("Unknown coach " + name + " (known: " + string.Join(", ", Names) + ")");
        }
    }

    /// <summary>A factory that creates the named coach for one match from its own stream.</summary>
    /// <param name="name">Coach name.</param>
    /// <param name="data">Loaded data (plays, systems, tuning).</param>
    /// <param name="systemId">Defensive system for <c>rotation</c>; ignored by coaches that choose their own.</param>
    public static Func<IRandom, ICoach> Factory(string name, GameData data, string systemId)
    {
        Require(name);
        if (name == Rotation)
        {
            var system = HarnessTeams.System(data, systemId);
            return _ => new RotationCoach(data.Plays, system);
        }

        var plays = data.Plays;
        var systems = data.Systems;
        var tuning = data.Tuning;
        return name switch
        {
            Random => random => new RandomCoach(plays, systems, tuning.Ai.Random, random),
            RandomTactics => random => new ComposedCoach(new RotationLines(), new AI.RandomTactics(plays, systems, tuning.Ai.Random, random)),
            RandomLines => random => new ComposedCoach(new AI.RandomLines(tuning.Ai.Random, random), new RuleTactics(plays, systems, tuning)),
            RuleBased => _ => new ComposedCoach(new RuleLines(tuning), new RuleTactics(plays, systems, tuning)),
            _ => _ => new ComposedCoach(new RuleLines(tuning), new AdaptiveTactics(plays, systems, tuning)),
        };
    }

    /// <summary>Label for output, e.g. <c>rotation/trap122</c> or <c>random</c>.</summary>
    public static string Label(string name, string systemId) => UsesSystemOption(name) ? name + "/" + systemId : name;
}
