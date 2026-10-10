using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Harness.Simulation;

/// <summary>
/// Harness-only deterministic team builder until roles.json exists (D-018). Every stat is <see cref="BaseStat"/> plus a
/// spread drawn from a generator seeded with the team seed, so the same seed always builds the same team. These are
/// test inputs for the harness, not balance values.
/// </summary>
public static class HarnessTeams
{
    /// <summary>Centre of the generated stats (the 1–20 scale's middle).</summary>
    public const int BaseStat = 10;

    /// <summary>Largest deviation from <see cref="BaseStat"/>.</summary>
    public const int Spread = 3;

    /// <summary>Builds a team (one trio, one pair, one goalie) with ids from <paramref name="firstId"/>.</summary>
    public static Team Build(string name, int firstId, ulong teamSeed)
    {
        var random = new Pcg32(teamSeed, 7UL);
        Skater Skater(int id, Position position) => new(id, name + " " + Positions.ToName(position), position, SkaterStats.FromArray(Stats(random, StatNames.SkaterStatCount)));

        Skater lw = Skater(firstId, Position.LeftWing);
        Skater c = Skater(firstId + 1, Position.Center);
        Skater rw = Skater(firstId + 2, Position.RightWing);
        Skater ld = Skater(firstId + 3, Position.LeftDefence);
        Skater rd = Skater(firstId + 4, Position.RightDefence);
        var goalie = new Goalie(firstId + 5, name + " G", GoalieStats.FromArray(Stats(random, StatNames.GoalieStatCount)));
        return new Team(name, new[] { lw, c, rw, ld, rd }, new[] { goalie }, new[] { new ForwardLine("F1", lw, c, rw) }, new[] { new DefencePair("D1", ld, rd) });
    }

    /// <summary>
    /// A shift setup with all repository plays in file order and the named systems (home and away). Home ids start at 1,
    /// away ids at 101.
    /// </summary>
    public static ShiftSetup Shift(GameData data, string homeSystem, string awaySystem, int maxSteps)
    {
        Team home = Build("Home", 1, 1UL);
        Team away = Build("Away", 101, 2UL);
        return new ShiftSetup(
            data.Rink,
            data.Tuning,
            TeamSetup(home, data, homeSystem),
            TeamSetup(away, data, awaySystem),
            "center",
            1,
            0.0,
            maxSteps);
    }

    private static TeamShiftSetup TeamSetup(Team team, GameData data, string systemId)
    {
        DefensiveSystem system = data.Systems.FirstOrDefault(s => s.Id == systemId)
            ?? throw new ArgumentException("Unknown system " + systemId + " (known: " + string.Join(", ", data.Systems.Select(s => s.Id)) + ")");
        return new TeamShiftSetup(
            new OnIceSkaters(team.ForwardLines[0], team.DefencePairs[0]),
            team.Goalies[0],
            new ShiftPlan(data.Plays, system));
    }

    private static int[] Stats(Pcg32 random, int count)
    {
        var values = new int[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = BaseStat + random.NextInt(-Spread, Spread + 1);
        }

        return values;
    }
}
