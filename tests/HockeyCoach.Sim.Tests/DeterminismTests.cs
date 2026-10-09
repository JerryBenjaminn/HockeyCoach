using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests;

/// <summary>
/// Same seed, same inputs: identical output. There is no match loop or event log yet (M2), so this runs
/// a random sequence of checks between two hand-built teams and compares the full result trace.
/// Extend to compare event logs once Sim.Events exists.
/// </summary>
public class DeterminismTests
{
    [Fact]
    public void SameSeed_ProducesIdenticalCheckTrace()
    {
        string first = RunTrace(seed: 42UL, steps: 2000);
        string second = RunTrace(seed: 42UL, steps: 2000);

        Assert.Equal(first, second);
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentCheckTraces()
    {
        Assert.NotEqual(RunTrace(seed: 42UL, steps: 200), RunTrace(seed: 43UL, steps: 200));
    }

    private static string RunTrace(ulong seed, int steps)
    {
        var rng = new Pcg32(seed);
        CheckFormulaConfig formula = TestChecks.Formula();
        CheckDefinition pass = TestChecks.Pass();
        CheckDefinition shot = TestChecks.SlotShot();
        Team home = BuildTeam("Home", 1, rng);
        Team away = BuildTeam("Away", 101, rng);

        var trace = new System.Text.StringBuilder();
        for (int i = 0; i < steps; i++)
        {
            bool homeAttacks = rng.Chance(0.5);
            Team attack = homeAttacks ? home : away;
            Team defend = homeAttacks ? away : home;
            CheckResult result;
            if (rng.NextInt(4) == 0)
            {
                var participants = new CheckParticipants()
                    .With("shooter", Pick(attack, rng))
                    .With("goalie", defend.Goalies[0]);
                result = CheckResolver.Resolve(formula, shot, participants, 0.0, rng);
            }
            else
            {
                var participants = new CheckParticipants()
                    .With("passer", Pick(attack, rng))
                    .With("receiver", Pick(attack, rng))
                    .With("nearestDefender", Pick(defend, rng));
                double modifier = rng.NextInt(3) - 1;
                result = CheckResolver.Resolve(formula, pass, participants, modifier, rng);
            }

            trace.Append(i).Append(';')
                .Append(result.CheckId).Append(';')
                .Append(result.Success ? 1 : 0).Append(';')
                .Append(result.Probability.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
        }

        return trace.ToString();
    }

    private static Skater Pick(Team team, IRandom rng)
    {
        return team.Skaters[rng.NextInt(team.Skaters.Count)];
    }

    private static Team BuildTeam(string name, int firstId, IRandom rng)
    {
        var skaters = new Skater[5];
        for (int i = 0; i < skaters.Length; i++)
        {
            var values = new int[StatNames.SkaterStatCount];
            for (int s = 0; s < values.Length; s++)
            {
                values[s] = rng.NextInt(1, 21);
            }

            skaters[i] = new Skater(firstId + i, name + i, i < 3 ? Position.Winger : Position.Defenseman, SkaterStats.FromArray(values));
        }

        var goalie = new Goalie(firstId + 5, name + "G", GoalieStats.Uniform(rng.NextInt(1, 21)));
        return new Team(name, skaters, new[] { goalie }, new[] { new Line("L1", skaters) });
    }
}
