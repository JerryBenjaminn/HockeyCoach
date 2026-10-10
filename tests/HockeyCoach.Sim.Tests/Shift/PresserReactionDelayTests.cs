using System.Text.Json.Nodes;
using HockeyCoach.Harness.Data;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Shift;

/// <summary>
/// D-074 presser reaction delay in whole shifts (data-schema.md, Puolustusjärjestelmät, Painostajan reaktioviive; Q-055
/// defaults). Every defence move is checked against an independent rule from the event log and an independent
/// recomputation of the move from the positions before it.
/// </summary>
public class PresserReactionDelayTests
{
    private static readonly string[] Spots = { "center", "offensiveLeft", "offensiveRight", "neutralOffensiveLeft", "neutralDefensiveRight", "defensiveLeft" };

    private sealed class Trace : IDefenceMoveTrace
    {
        public List<DefenceMoveSample> Samples { get; } = new();

        public void OnDefenceMove(DefenceMoveSample sample) => Samples.Add(sample);
    }

    /// <summary>One defence move with the log events since the previous defence move of the shift.</summary>
    private sealed record Move(ShiftSetup Setup, bool Pinch, DefenceMoveSample Sample, DefenceMoveSample? Previous, IReadOnlyList<SimEvent> Window);

    /// <summary>The repository data with <c>pressure.presserReactionNodes</c> replaced (null = unchanged).</summary>
    private static GameData DataWith(int? reactionNodes)
    {
        if (reactionNodes == null)
        {
            return ShiftTestData.Data;
        }

        string dataDir = Path.Combine(Fixtures.RepoRoot(), "data");
        JsonObject root = JsonNode.Parse(File.ReadAllText(Path.Combine(dataDir, "tuning.json")))!.AsObject();
        root["pressure"]!["presserReactionNodes"] = reactionNodes.Value;
        string file = Path.Combine(Path.GetTempPath(), "hc-tuning-reaction-" + reactionNodes.Value + "-" + Environment.ProcessId + ".json");
        File.WriteAllText(file, root.ToJsonString());
        try
        {
            return GameData.Load(dataDir, file);
        }
        finally
        {
            File.Delete(file);
        }
    }

    private static ShiftSetup AtSpot(GameData data, string spot, bool pinch, string home, string away)
    {
        ShiftSetup s = HarnessTeams.Shift(data, home, away, 2000);
        TeamShiftSetup Team(TeamShiftSetup t) => new(
            t.Skaters,
            t.Goalie,
            new ShiftPlan(t.Plan.Plays, t.Plan.System, t.Plan.Transition, new SystemModeInstructions(false, 1, pinch)));
        return new ShiftSetup(s.Rink, s.Tuning, Team(s.Home), Team(s.Away), spot, 1, 0.0, s.MaxSteps);
    }

    private static List<Move> Moves(GameData data, bool pinch, string home, string away, ulong seeds)
    {
        var moves = new List<Move>();
        foreach (string spot in Spots.Where(s => data.Rink.FaceoffSpots.Any(f => f.Id == s)))
        {
            ShiftSetup setup = AtSpot(data, spot, pinch, home, away);
            for (ulong seed = 1; seed <= seeds; seed++)
            {
                var trace = new Trace();
                List<SimEvent> events = ShiftSimulator.Run(setup, new Pcg32(seed), trace).Log.Events.ToList();
                for (int i = 0; i < trace.Samples.Count; i++)
                {
                    DefenceMoveSample? previous = i > 0 ? trace.Samples[i - 1] : null;
                    int from = previous?.LogCount ?? 0;
                    DefenceMoveSample sample = trace.Samples[i];
                    moves.Add(new Move(setup, pinch, sample, previous, events.Skip(from).Take(sample.LogCount - from).ToList()));
                }
            }
        }

        return moves;
    }

    private static List<Move> Set(GameData data) =>
        Moves(data, false, "forecheck212", "trap122", 80).Concat(Moves(data, true, "trap122", "forecheck212", 80)).ToList();

    private static readonly Lazy<List<Move>> Default = new(() => Set(ShiftTestData.Data));

    private static readonly Lazy<List<Move>> Zero = new(() => Set(DataWith(0)));

    private static readonly Lazy<List<Move>> Full = new(() => Set(DataWith(2)));

    /// <summary>
    /// The spec rule from the log alone: the move is a beat's or rush action's defence move, a completed pass happened
    /// since the previous defence move, and after that pass nothing but its zone entry event was logged (no turnover,
    /// line change, stoppage, other puck action).
    /// </summary>
    private static PassEvent? TriggeringPass(Move m)
    {
        if (m.Sample.Kind != DefenceMoveKind.Action)
        {
            return null;
        }

        int last = -1;
        for (int i = 0; i < m.Window.Count; i++)
        {
            if (m.Window[i] is PassEvent { Succeeded: true })
            {
                last = i;
            }
        }

        if (last < 0 || m.Window.Skip(last + 1).Any(e => e is not ControlledZoneEntryEvent))
        {
            return null;
        }

        return (PassEvent)m.Window[last];
    }

    private static bool CompletedPassSincePreviousMove(Move m) => m.Window.Any(e => e is PassEvent { Succeeded: true });

    private static bool PinchSpot(Rink rink, GridPoint puck)
    {
        int offensiveMin = rink.Zones.First(z => z.Zone == RinkZone.Offensive).XMin;
        return (puck.Y == 0 || puck.Y == rink.Width - 1) && puck.X >= offensiveMin && puck.X < rink.OpponentGoal.X;
    }

    private static TeamShiftSetup Defence(Move m) => m.Sample.Defending == TeamSide.Home ? m.Setup.Home : m.Setup.Away;

    /// <summary>M-7 presser before the move (point 4): the nearest defender in a rush, otherwise the system rule.</summary>
    private static Position ExpectedPresser(Move m)
    {
        DefenceMoveSample s = m.Sample;
        Position presser;
        if (s.Rush)
        {
            Assert.True(PresserSelector.Nearest(s.Before, s.Puck, out presser));
        }
        else
        {
            bool pinched = m.Pinch && PinchSpot(m.Setup.Rink, s.Puck);
            Assert.True(PresserSelector.FromSystem(Defence(m).Plan.System, m.Setup.Rink, s.Puck, PuckState.Controlled, s.Before, pinched, out presser));
        }

        return presser;
    }

    /// <summary>The system move recomputed from the positions before it, with <paramref name="limited"/> taking at most <paramref name="limitedSteps"/>.</summary>
    private static IReadOnlyDictionary<Position, GridPoint> Recompute(Move m, Position? limited, int limitedSteps)
    {
        Rink rink = m.Setup.Rink;
        DefenceMoveSample s = m.Sample;
        int maxSteps = m.Setup.Tuning.Plays.MaxNodesPerBeat;
        SystemTargets targets = SystemTargetResolver.Resolve(Defence(m).Plan.System, rink, s.Puck, PuckState.Controlled, s.Before);
        bool pinched = m.Pinch && PinchSpot(rink, s.Puck);
        var moves = new List<OccupancyMove>();
        foreach (SystemRole role in OccupancyResolver.SystemOrder)
        {
            Position position = targets.Roles[role];
            GridPoint target = pinched && role == SystemRole.D1 ? s.Puck : targets.Targets[position];
            int steps = position == limited ? Math.Min(maxSteps, limitedSteps) : maxSteps;
            moves.Add(new OccupancyMove(position, SystemMovement.Path(s.Before[position], target, s.Puck, steps, rink)));
        }

        return OccupancyResolver.Resolve(s.Before, moves, rink);
    }

    [Fact]
    public void Delay_AppliesExactlyToTheBeatOrRushMoveRightAfterACompletedPass()
    {
        List<Move> moves = Default.Value;
        foreach (Move m in moves)
        {
            bool expected = TriggeringPass(m) != null;
            Assert.True(expected == m.Sample.DelayedPresser.HasValue, "move at log " + m.Sample.LogCount + ": expected delay " + expected);
        }

        Assert.Contains(moves, m => m.Sample.DelayedPresser.HasValue && m.Sample.Rush);
        Assert.Contains(moves, m => m.Sample.DelayedPresser.HasValue && !m.Sample.Rush);
        Assert.Contains(moves, m => m.Sample.Kind == DefenceMoveKind.Action && !m.Sample.DelayedPresser.HasValue);
    }

    [Fact]
    public void Delay_LapsesWhenASystemStepSetupOrBattleComesBeforeTheNextAction()
    {
        List<Move> moves = Default.Value;

        Assert.All(moves.Where(m => m.Sample.Kind == DefenceMoveKind.Other), m => Assert.Null(m.Sample.DelayedPresser));

        // A completed pass followed directly by a non-beat move (the play's last beat was a pass, or the rush ended).
        Assert.Contains(moves, m => m.Sample.Kind == DefenceMoveKind.Other && CompletedPassSincePreviousMove(m)
            && m.Window.SkipWhile(e => e is not PassEvent { Succeeded: true }).Skip(1).All(e => e is ControlledZoneEntryEvent));

        // The next beat after such a lapse is not delayed unless a new pass came in between (D-036 entry pass).
        for (int i = 1; i < moves.Count; i++)
        {
            Move m = moves[i];
            if (m.Sample.Kind == DefenceMoveKind.Action && m.Previous != null && m.Previous.Kind == DefenceMoveKind.Other && !CompletedPassSincePreviousMove(m))
            {
                Assert.Null(m.Sample.DelayedPresser);
            }
        }
    }

    [Fact]
    public void Delay_CoversRushPassesPlayBeatPassesAndTheD036EntryPass()
    {
        List<Move> delayed = Default.Value.Where(m => m.Sample.DelayedPresser.HasValue).ToList();

        Assert.Contains(delayed, m => TriggeringPass(m)!.Context.PlayId == null);
        Assert.Contains(delayed, m => m.Previous?.Kind == DefenceMoveKind.Other && TriggeringPass(m)!.Context.PlayId != null);
        Assert.Contains(delayed, m => m.Previous?.Kind == DefenceMoveKind.Action && TriggeringPass(m)!.Context.PlayId != null);
    }

    /// <summary>
    /// Runs shifts at <paramref name="spot"/> with <paramref name="play"/> first in the home plays (replacing home faceoff
    /// plays) and checks that the defence move right after each completed pass of the play is delayed. Returns how many
    /// such passes were seen.
    /// </summary>
    private static int DelayedAfterPassesOf(Play play, string spot, Func<PassEvent, bool> which)
    {
        ShiftSetup s = AtSpot(ShiftTestData.Data, spot, false, "forecheck212", "trap122");
        var plays = new List<Play> { play };
        plays.AddRange(s.Home.Plan.Plays.Where(p => p.Type != PlayType.Faceoff));
        var home = new TeamShiftSetup(s.Home.Skaters, s.Home.Goalie, new ShiftPlan(plays, s.Home.Plan.System, s.Home.Plan.Transition, s.Home.Plan.Instructions));
        var setup = new ShiftSetup(s.Rink, s.Tuning, home, s.Away, s.FaceoffSpotId, s.Period, s.StartTime, s.MaxSteps);

        int seen = 0;
        for (ulong seed = 1; seed <= 80; seed++)
        {
            var trace = new Trace();
            List<SimEvent> events = ShiftSimulator.Run(setup, new Pcg32(seed), trace).Log.Events.ToList();
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] is not PassEvent { Succeeded: true } pass || pass.Context.PlayId != play.Id || !which(pass))
                {
                    continue;
                }

                DefenceMoveSample? next = trace.Samples.FirstOrDefault(x => x.LogCount > i);
                if (next == null || events.Skip(i + 1).Take(next.LogCount - i - 1).Any(e => e is not ControlledZoneEntryEvent))
                {
                    continue;
                }

                Assert.Equal(DefenceMoveKind.Action, next.Kind);
                Assert.NotNull(next.DelayedPresser);
                seen++;
            }
        }

        return seen;
    }

    [Fact]
    public void Delay_AppliesAfterAFaceoffPlayPass()
    {
        // The repository's faceoff play has no pass, so a test faceoff play passes D to D and shoots (D-060).
        Play faceoffPass = TestPlays.Build(
            Position.LeftDefence,
            new[]
            {
                TestPlays.At(Position.Center, 8, 1), TestPlays.At(Position.LeftWing, 8, 0), TestPlays.At(Position.RightWing, 8, 3),
                TestPlays.At(Position.LeftDefence, 7, 1), TestPlays.At(Position.RightDefence, 7, 3),
            },
            new[]
            {
                new Beat(TestPlays.NoMoves, PlayAction.Pass(Position.LeftDefence, Position.RightDefence)),
                new Beat(TestPlays.NoMoves, PlayAction.Shoot(Position.RightDefence)),
            },
            PlayType.Faceoff,
            "offensiveLeft",
            "testFaceoffPass");

        Assert.True(DelayedAfterPassesOf(faceoffPass, "offensiveLeft", p => true) > 0, "no completed faceoff-play pass");
    }

    [Fact]
    public void Delay_AppliesAfterABreakoutCheckPass()
    {
        // The repository's breakout passes stay inside the defensive zone, so a test play passes out of it (D-039 breakout check).
        Play breakoutPass = TestPlays.Build(
            Position.LeftDefence,
            new[]
            {
                TestPlays.At(Position.LeftDefence, 3, 1), TestPlays.At(Position.LeftWing, 4, 0), TestPlays.At(Position.Center, 2, 2),
                TestPlays.At(Position.RightWing, 4, 4), TestPlays.At(Position.RightDefence, 2, 3),
            },
            new[]
            {
                new Beat(TestPlays.NoMoves, PlayAction.Pass(Position.LeftDefence, Position.LeftWing)),
                new Beat(TestPlays.NoMoves, PlayAction.Skate(Position.LeftWing, new GridPoint(6, 0))),
            },
            PlayType.Breakout,
            null,
            "testBreakoutPass");
        Rink rink = ShiftTestData.Data.Rink;

        bool Breakout(PassEvent pass)
        {
            PlayerPlacement passer = pass.Context.Placement.Find(pass.PasserId);
            PlayerPlacement receiver = pass.Context.Placement.Find(pass.ReceiverId);
            GridPoint Own(PlayerPlacement p) => TeamFrame.ToTeamView(new GridPoint(p.NodeId / rink.Width, p.NodeId % rink.Width), passer.Team, rink);
            return rink.ZoneAtX(Own(passer).X) == RinkZone.Defensive && rink.ZoneAtX(Own(receiver).X) != RinkZone.Defensive;
        }

        Assert.True(DelayedAfterPassesOf(breakoutPass, "defensiveLeft", Breakout) > 0, "no completed breakout-check pass");
    }

    [Fact]
    public void DelayedPresser_IsTheM7PresserBeforeTheMove_WithTheReceiverAsCarrier()
    {
        foreach (Move m in Default.Value.Where(m => m.Sample.DelayedPresser.HasValue))
        {
            PassEvent pass = TriggeringPass(m)!;
            GridPoint puck = TeamFrame.ToRink(m.Sample.Puck, m.Sample.Defending, m.Setup.Rink);
            Assert.Equal(m.Setup.Rink.IdOf(puck.X, puck.Y), pass.Context.Placement.Find(pass.ReceiverId).NodeId);
            Assert.Equal(ExpectedPresser(m), m.Sample.DelayedPresser!.Value);
        }
    }

    [Fact]
    public void DelayedMove_PresserTakesAtMostReactionNodes_OthersUnchangedRules()
    {
        List<Move> delayed = Default.Value.Where(m => m.Sample.DelayedPresser.HasValue).ToList();
        int n = ShiftTestData.Data.Tuning.Pressure.PresserReactionNodes;
        Assert.Equal(1, n);
        foreach (Move m in delayed)
        {
            Assert.Equal(Recompute(m, m.Sample.DelayedPresser, n), m.Sample.After);
        }

        // The delay changes where the presser ends up in some moves.
        Assert.Contains(delayed, m => !Recompute(m, null, 0)[m.Sample.DelayedPresser!.Value].Equals(m.Sample.After[m.Sample.DelayedPresser!.Value]));

        // Undelayed moves follow the ordinary rule.
        foreach (Move m in Default.Value.Where(m => m.Sample.Kind == DefenceMoveKind.Action && !m.Sample.DelayedPresser.HasValue).Take(2000))
        {
            Assert.Equal(Recompute(m, null, 0), m.Sample.After);
        }
    }

    [Fact]
    public void ZeroReactionNodes_PresserDoesNotMoveAfterAPass()
    {
        List<Move> delayed = Zero.Value.Where(m => m.Sample.DelayedPresser.HasValue).ToList();
        Assert.NotEmpty(delayed);
        foreach (Move m in delayed)
        {
            Assert.Equal(Recompute(m, m.Sample.DelayedPresser, 0), m.Sample.After);
        }

        Assert.Contains(delayed, m =>
        {
            Position presser = m.Sample.DelayedPresser!.Value;
            return m.Sample.After[presser].Equals(m.Sample.Before[presser]) && !Recompute(m, null, 0)[presser].Equals(m.Sample.Before[presser]);
        });
    }

    [Fact]
    public void ReactionNodesAtMaxNodesPerBeat_HasNoEffectOnTheMove()
    {
        List<Move> delayed = Full.Value.Where(m => m.Sample.DelayedPresser.HasValue).ToList();
        Assert.NotEmpty(delayed);
        foreach (Move m in delayed)
        {
            Assert.Equal(Recompute(m, null, 0), m.Sample.After);
        }
    }

    [Fact]
    public void SameSeed_GivesTheSameMovesAndLog()
    {
        ShiftSetup setup = AtSpot(ShiftTestData.Data, "neutralOffensiveLeft", true, "forecheck212", "trap122");
        for (ulong seed = 1; seed <= 30; seed++)
        {
            string Run()
            {
                var trace = new Trace();
                ShiftResult result = ShiftSimulator.Run(setup, new Pcg32(seed), trace);
                string moves = string.Join(";", trace.Samples.Select(s => s.Kind + ":" + s.DelayedPresser + ":" + string.Join(",", s.After.Values)));
                string log = string.Join(";", result.Log.Events.Select(e => e.GetType().Name + "@" + e.Context.Time + ":" + e.Context.Placement.PuckNodeId));
                return moves + "|" + log;
            }

            Assert.Equal(Run(), Run());
        }
    }
}
