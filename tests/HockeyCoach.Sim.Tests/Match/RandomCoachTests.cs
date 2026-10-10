using HockeyCoach.AI;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Shift;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Match;

/// <summary><c>random</c> coach (D-077 item 2, data-schema.md AI-2 R-L and R-T).</summary>
public class RandomCoachTests
{
    private static readonly Team Team = TestPlayers.Team("Home", 1, trios: 4, pairs: 3);

    private static RandomCoach Coach(IRandom random, double onTheFly = 0.5) =>
        new(ShiftTestData.Data.Plays, ShiftTestData.Data.Systems, new AiRandomConfig(onTheFly, 0.02), random);

    [Fact]
    public void AtStoppage_DrawsInTheSpecifiedOrder()
    {
        IReadOnlyList<Play> library = ShiftTestData.Data.Plays;
        DefensiveSystem[] systems = ShiftTestData.Data.Systems.OrderBy(s => s.Id, StringComparer.Ordinal).ToArray();
        CoachDecision decision = Coach(new Pcg32(99UL)).AtStoppage(TestCoachViews.Make(Team, -1, -1, true, true));

        // Replay the AI-2 draw order on a twin stream: trio, pair, Fisher–Yates, system, transition, netFront, chasers, pinch.
        var twin = new Pcg32(99UL);
        int trio = twin.NextInt(4);
        int pair = twin.NextInt(3);
        Play[] plays = library.ToArray();
        for (int i = plays.Length - 1; i >= 1; i--)
        {
            int j = twin.NextInt(i + 1);
            (plays[i], plays[j]) = (plays[j], plays[i]);
        }

        DefensiveSystem system = systems[twin.NextInt(systems.Length)];
        TransitionInstruction transition = twin.NextInt(2) == 0 ? TransitionInstruction.Rush : TransitionInstruction.Regroup;
        bool netFront = twin.NextInt(2) == 1;
        int chasers = 1 + twin.NextInt(2);
        bool pinch = twin.NextInt(2) == 1;

        Assert.Equal(trio, decision.Units.ForwardIndex);
        Assert.Equal(pair, decision.Units.PairIndex);
        Assert.Equal(plays.Select(p => p.Id), decision.Plan.Plays.Select(p => p.Id));
        Assert.Same(system, decision.Plan.System);
        Assert.Equal(transition, decision.Plan.Transition);
        Assert.Equal(netFront, decision.Plan.Instructions.NetFrontAfterShot);
        Assert.Equal(chasers, decision.Plan.Instructions.LooseChasers);
        Assert.Equal(pinch, decision.Plan.Instructions.Pinch);
    }

    [Fact]
    public void AtStoppage_KeepsIneligibleUnitsWithoutADraw()
    {
        var random = new Pcg32(5UL);
        var twin = new Pcg32(5UL);
        CoachDecision decision = Coach(random).AtStoppage(TestCoachViews.Make(Team, 2, 1, false, false, call: CoachCall.Stoppage));

        Assert.Equal(2, decision.Units.ForwardIndex);
        Assert.Equal(1, decision.Units.PairIndex);

        // Only the tactics were drawn: the play order is the Fisher–Yates order of an untouched twin stream.
        Play[] plays = ShiftTestData.Data.Plays.ToArray();
        for (int i = plays.Length - 1; i >= 1; i--)
        {
            int j = twin.NextInt(i + 1);
            (plays[i], plays[j]) = (plays[j], plays[i]);
        }

        Assert.Equal(plays.Select(p => p.Id), decision.Plan.Plays.Select(p => p.Id));
    }

    [Fact]
    public void AtStoppage_CoversEveryUnitPlayOrderSystemAndInstruction_OverManyDraws()
    {
        RandomCoach coach = Coach(new Pcg32(1UL));
        var trios = new HashSet<int>();
        var pairs = new HashSet<int>();
        var firstPlays = new HashSet<string>();
        var systems = new HashSet<string>();
        var transitions = new HashSet<TransitionInstruction>();
        var instructions = new HashSet<(bool, int, bool)>();
        for (int i = 0; i < 400; i++)
        {
            CoachDecision d = coach.AtStoppage(TestCoachViews.Make(Team, 0, 0, true, true, call: CoachCall.Stoppage));
            trios.Add(d.Units.ForwardIndex);
            pairs.Add(d.Units.PairIndex);
            firstPlays.Add(d.Plan.Plays[0].Id);
            systems.Add(d.Plan.System.Id);
            transitions.Add(d.Plan.Transition);
            instructions.Add((d.Plan.Instructions.NetFrontAfterShot, d.Plan.Instructions.LooseChasers, d.Plan.Instructions.Pinch));
            Assert.Equal(ShiftTestData.Data.Plays.Select(p => p.Id).OrderBy(x => x, StringComparer.Ordinal), d.Plan.Plays.Select(p => p.Id).OrderBy(x => x, StringComparer.Ordinal));
        }

        Assert.Equal(4, trios.Count);
        Assert.Equal(3, pairs.Count);
        Assert.Equal(ShiftTestData.Data.Plays.Count, firstPlays.Count);
        Assert.Equal(ShiftTestData.Data.Systems.Count, systems.Count);
        Assert.Equal(2, transitions.Count);
        Assert.Equal(8, instructions.Count);
    }

    [Fact]
    public void OnTheFly_WithProbabilityZero_NeverChanges_AndDrawsOneChancePerEligibleType()
    {
        var random = new Pcg32(3UL);
        RandomCoach coach = Coach(random, onTheFly: 0.0);
        LineChoice choice = coach.OnTheFly(TestCoachViews.Make(Team, 2, 1, true, true, call: CoachCall.OnTheFly));

        Assert.Equal(2, choice.ForwardIndex);
        Assert.Equal(1, choice.PairIndex);
        var twin = new Pcg32(3UL);
        twin.NextDouble();
        twin.NextDouble();
        Assert.Equal(twin.NextUInt(), random.NextUInt());
    }

    [Fact]
    public void OnTheFly_WithProbabilityOne_AlwaysPicksABenchUnit_AndReachesEveryBenchUnit()
    {
        RandomCoach coach = Coach(new Pcg32(8UL), onTheFly: 1.0);
        var trios = new HashSet<int>();
        var pairs = new HashSet<int>();
        for (int i = 0; i < 200; i++)
        {
            LineChoice choice = coach.OnTheFly(TestCoachViews.Make(Team, 1, 2, true, true, call: CoachCall.OnTheFly));
            trios.Add(choice.ForwardIndex);
            pairs.Add(choice.PairIndex);
        }

        Assert.Equal(new[] { 0, 2, 3 }, trios.OrderBy(x => x));
        Assert.Equal(new[] { 0, 1 }, pairs.OrderBy(x => x));
    }

    [Fact]
    public void OnTheFly_KeepsIneligibleUnitsWithoutADraw()
    {
        var random = new Pcg32(4UL);
        LineChoice choice = Coach(random, onTheFly: 1.0).OnTheFly(TestCoachViews.Make(Team, 3, 0, false, false, call: CoachCall.OnTheFly));

        Assert.Equal(3, choice.ForwardIndex);
        Assert.Equal(0, choice.PairIndex);
        Assert.Equal(new Pcg32(4UL).NextUInt(), random.NextUInt());
    }

    [Theory]
    [InlineData(CoachRegistry.Random, CoachRegistry.Rotation)]
    [InlineData(CoachRegistry.Rotation, CoachRegistry.Random)]
    [InlineData(CoachRegistry.Random, CoachRegistry.Random)]
    public void Matches_WithRandomCoaches_AreDeterministicAndDoNotStall(string home, string away)
    {
        var data = ShiftTestData.Data;
        string Log(ulong seed) => MatchJson.Write(MatchCommand.Play(data, MatchTestData.Setup, home, "forecheck212", away, "trap122", seed), MatchTestData.Setup, seed);

        Assert.Equal(Log(17UL), Log(17UL));
        Assert.NotEqual(Log(17UL), Log(18UL));
        for (ulong seed = 1; seed <= 10; seed++)
        {
            Assert.False(MatchCommand.Play(data, MatchTestData.Setup, home, "forecheck212", away, "trap122", seed).Stalled, "stalled at seed " + seed);
        }
    }

    [Fact]
    public void Parse_AcceptsCoachNames_AndRejectsUnknownOnes()
    {
        MatchCommand.Options? options = MatchCommand.Parse(new[] { "match", "--home", "random", "--away", "rotation" }, out string? error);
        Assert.Null(error);
        Assert.Equal("random", options!.HomeCoach);
        Assert.Equal("rotation", options.AwayCoach);

        Assert.Null(MatchCommand.Parse(new[] { "match", "--home", "rulebased" }, out error));
        Assert.Contains("Unknown coach rulebased", error);
    }
}
