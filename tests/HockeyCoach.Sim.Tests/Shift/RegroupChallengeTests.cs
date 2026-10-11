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
/// The own-zone regroup challenge (data-schema.md O-7, "Karvaus haastaa kokoamisen omassa päässä", D-078 K1): the pure
/// rules, and whole shifts where every regroup move is checked against an independent recomputation from the trace and log.
/// </summary>
public class RegroupChallengeTests
{
    private static readonly string[] Spots = { "center", "offensiveLeft", "offensiveRight", "neutralOffensiveLeft", "neutralDefensiveRight", "defensiveLeft" };

    private static readonly Position[] TieOrder = { Position.Center, Position.LeftWing, Position.RightWing, Position.LeftDefence, Position.RightDefence };

    private sealed class Trace : IDefenceMoveTrace
    {
        public List<DefenceMoveSample> Samples { get; } = new();

        public void OnDefenceMove(DefenceMoveSample sample) => Samples.Add(sample);
    }

    /// <summary>One defence move with the log of its shift and the next defence move of the shift (if any).</summary>
    private sealed record Move(ShiftSetup Setup, DefenceMoveSample Sample, DefenceMoveSample? Next, IReadOnlyList<SimEvent> Events);

    // ------------------------------------------------------------------ pure rules

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 0, true)]
    [InlineData(1, 1, false)]
    [InlineData(2, 1, true)]
    [InlineData(3, 2, true)]
    [InlineData(3, 3, false)]
    public void IsChallenged_OnlyWhenForecheckersExceedTheAllowance(int forecheckers, int allowed, bool expected)
    {
        Assert.Equal(expected, RegroupChallenge.IsChallenged(forecheckers, allowed));
    }

    [Fact]
    public void SetupStepsAfterRegroup_MakeTheRegroupAndSetupTogetherMoveTheTotal()
    {
        // Repository data: r = 2 / 3, n_tot = floor(14 r) = 9, n_reg = floor(8 r) = 5, n_set = 4.
        Assert.Equal(4, RegroupChallenge.SetupStepsAfterRegroup(9, 5, 55));
        Assert.Equal(9, 5 + RegroupChallenge.SetupStepsAfterRegroup(9, 5, 55));
        Assert.Equal(55, RegroupChallenge.SetupStepsAfterRegroup(55, 55, 55));
    }

    [Fact]
    public void SetupStepsAfterRegroup_MatchTheRepositoryTuning()
    {
        var t = ShiftTestData.Data.Tuning;
        double r = t.Plays.MaxNodesPerBeat / t.Time.GetSecondsPerAction("skate");
        int total = (int)Math.Floor(((t.Time.RegroupSeconds + t.Time.SetupSeconds) * r) + 1e-9);
        int regroup = (int)Math.Floor((t.Time.RegroupSeconds * r) + 1e-9);

        Assert.Equal((9, 5), (total, regroup));
        Assert.Equal(total, regroup + RegroupChallenge.SetupStepsAfterRegroup(total, regroup, 55));
    }

    [Fact]
    public void ForecheckersInZone_CountsOnlyF1ToF3InTheAttackersDefensiveZone_WithoutFallback()
    {
        Rink rink = ShiftTestData.Data.Rink;
        var roles = new Dictionary<SystemRole, Position>
        {
            [SystemRole.F1] = Position.Center,
            [SystemRole.F2] = Position.LeftWing,
            [SystemRole.F3] = Position.RightWing,
            [SystemRole.D1] = Position.LeftDefence,
            [SystemRole.D2] = Position.RightDefence,
        };
        int defensiveMax = rink.Zones.First(z => z.Zone == RinkZone.Defensive).XMax;
        int neutral = defensiveMax + 1;

        // Attacker's view: C and RW in the defensive zone, LW and both defencemen outside.
        var nodes = new Dictionary<Position, GridPoint>
        {
            [Position.Center] = new GridPoint(1, 2),
            [Position.LeftWing] = new GridPoint(neutral, 1),
            [Position.RightWing] = new GridPoint(defensiveMax, 3),
            [Position.LeftDefence] = new GridPoint(0, 1),
            [Position.RightDefence] = new GridPoint(1, 3),
        };
        Assert.Equal(new[] { Position.Center, Position.RightWing }, RegroupChallenge.ForecheckersInZone(roles, nodes, rink));

        foreach (Position p in new[] { Position.Center, Position.RightWing })
        {
            nodes[p] = new GridPoint(neutral, p == Position.Center ? 2 : 3);
        }

        Assert.Empty(RegroupChallenge.ForecheckersInZone(roles, nodes, rink));
    }

    [Fact]
    public void NearestForechecker_UsesChebyshevThenManhattanThenCLwRw_AmongTheCountedOnly()
    {
        var nodes = new Dictionary<Position, GridPoint>
        {
            [Position.Center] = new GridPoint(3, 2),
            [Position.LeftWing] = new GridPoint(2, 0),
            [Position.RightWing] = new GridPoint(2, 3),
            [Position.LeftDefence] = new GridPoint(1, 1),
            [Position.RightDefence] = new GridPoint(5, 3),
        };
        var carrier = new GridPoint(1, 1);

        // LD is on the carrier's node but not a counted forechecker.
        // Chebyshev: C 2, LW 1, RW 2 → LW.
        Assert.Equal(Position.LeftWing, RegroupChallenge.NearestForechecker(new[] { Position.Center, Position.LeftWing, Position.RightWing }, nodes, carrier));

        // C (2, 2): Chebyshev 1, Manhattan 2; RW (2, 1): Chebyshev 1, Manhattan 1 → RW.
        nodes[Position.Center] = new GridPoint(2, 2);
        nodes[Position.RightWing] = new GridPoint(2, 1);
        Assert.Equal(Position.RightWing, RegroupChallenge.NearestForechecker(new[] { Position.Center, Position.RightWing }, nodes, carrier));

        // Full tie (both Chebyshev 1, Manhattan 1): C before LW before RW.
        nodes[Position.Center] = new GridPoint(1, 2);
        nodes[Position.LeftWing] = new GridPoint(1, 0);
        Assert.Equal(Position.Center, RegroupChallenge.NearestForechecker(new[] { Position.RightWing, Position.LeftWing, Position.Center }, nodes, carrier));
        Assert.Equal(Position.LeftWing, RegroupChallenge.NearestForechecker(new[] { Position.RightWing, Position.LeftWing }, nodes, carrier));
    }

    // ------------------------------------------------------------------ whole shifts

    /// <summary>The repository data with <c>transitions.regroupForecheckersAllowed</c> replaced (null = unchanged).</summary>
    private static GameData DataWith(int? allowed)
    {
        if (allowed == null)
        {
            return ShiftTestData.Data;
        }

        string dataDir = Path.Combine(Fixtures.RepoRoot(), "data");
        JsonObject root = JsonNode.Parse(File.ReadAllText(Path.Combine(dataDir, "tuning.json")))!.AsObject();
        root["transitions"]!["regroupForecheckersAllowed"] = allowed.Value;
        string file = Path.Combine(Path.GetTempPath(), "hc-tuning-regroup-" + allowed.Value + "-" + Environment.ProcessId + ".json");
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

    /// <summary>Both teams regroup (pinch off); <paramref name="home"/> and <paramref name="away"/> are the systems.</summary>
    private static ShiftSetup RegroupSetup(GameData data, string spot, string home, string away)
    {
        ShiftSetup s = HarnessTeams.Shift(data, home, away, 2000);
        TeamShiftSetup Team(TeamShiftSetup t) => new(
            t.Skaters,
            t.Goalie,
            new ShiftPlan(t.Plan.Plays, t.Plan.System, TransitionInstruction.Regroup, new SystemModeInstructions(true, 1, false)));
        return new ShiftSetup(s.Rink, s.Tuning, Team(s.Home), Team(s.Away), spot, 1, 0.0, s.MaxSteps);
    }

    private static List<Move> Moves(GameData data, ulong seeds)
    {
        var moves = new List<Move>();
        foreach ((string home, string away) in new[] { ("forecheck212", "trap122"), ("trap122", "forecheck212"), ("forecheck212", "forecheck212") })
        {
            foreach (string spot in Spots.Where(s => data.Rink.FaceoffSpots.Any(f => f.Id == s)))
            {
                ShiftSetup setup = RegroupSetup(data, spot, home, away);
                for (ulong seed = 1; seed <= seeds; seed++)
                {
                    var trace = new Trace();
                    List<SimEvent> events = ShiftSimulator.Run(setup, new Pcg32(seed), trace).Log.Events.ToList();
                    for (int i = 0; i < trace.Samples.Count; i++)
                    {
                        moves.Add(new Move(setup, trace.Samples[i], i + 1 < trace.Samples.Count ? trace.Samples[i + 1] : null, events));
                    }
                }
            }
        }

        return moves;
    }

    private static readonly Lazy<List<Move>> Default = new(() => Moves(ShiftTestData.Data, 60));

    private static TeamShiftSetup DefenceOf(Move m) => m.Sample.Defending == TeamSide.Home ? m.Setup.Home : m.Setup.Away;

    private static TeamShiftSetup AttackOf(Move m) => m.Sample.Defending == TeamSide.Home ? m.Setup.Away : m.Setup.Home;

    /// <summary>A defending-view node in the attacking team's view.</summary>
    private static GridPoint AttackerView(Move m, GridPoint defendingView)
    {
        Rink rink = m.Setup.Rink;
        TeamSide attacking = TeamSides.Opponent(m.Sample.Defending);
        return TeamFrame.ToTeamView(TeamFrame.ToRink(defendingView, m.Sample.Defending, rink), attacking, rink);
    }

    /// <summary>Forecheckers after the move, recomputed independently: F1-F3 of the defence in the attacker's defensive zone.</summary>
    private static List<Position> CountedForecheckers(Move m)
    {
        Rink rink = m.Setup.Rink;
        IReadOnlyDictionary<SystemRole, Position> roles = RoleAssigner.Assign(m.Sample.After, m.Sample.Puck, rink);
        return new[] { SystemRole.F1, SystemRole.F2, SystemRole.F3 }
            .Select(r => roles[r])
            .Where(p => rink.ZoneAtX(AttackerView(m, m.Sample.After[p]).X) == RinkZone.Defensive)
            .ToList();
    }

    /// <summary>The spec's nearest forechecker, written out: Chebyshev, Manhattan, then C, LW, RW.</summary>
    private static Position ExpectedTaker(Move m, List<Position> forecheckers)
    {
        GridPoint carrier = m.Sample.Puck;
        return forecheckers
            .OrderBy(p => Math.Max(Math.Abs(m.Sample.After[p].X - carrier.X), Math.Abs(m.Sample.After[p].Y - carrier.Y)))
            .ThenBy(p => Math.Abs(m.Sample.After[p].X - carrier.X) + Math.Abs(m.Sample.After[p].Y - carrier.Y))
            .ThenBy(p => Array.IndexOf(TieOrder, p))
            .First();
    }

    /// <summary>The system move recomputed from the positions before it with <paramref name="steps"/> per defender (no pinch).</summary>
    private static IReadOnlyDictionary<Position, GridPoint> Recompute(ShiftSetup setup, DefensiveSystem system, DefenceMoveSample s, int steps)
    {
        Rink rink = setup.Rink;
        SystemTargets targets = SystemTargetResolver.Resolve(system, rink, s.Puck, PuckState.Controlled, s.Before);
        var moves = new List<OccupancyMove>();
        foreach (SystemRole role in OccupancyResolver.SystemOrder)
        {
            Position position = targets.Roles[role];
            moves.Add(new OccupancyMove(position, SystemMovement.Path(s.Before[position], targets.Targets[position], s.Puck, steps, rink)));
        }

        return OccupancyResolver.Resolve(s.Before, moves, rink);
    }

    /// <summary>The first event logged after the regroup move and before the next defence move, if any.</summary>
    private static SimEvent? FirstEventAfter(Move m)
    {
        int end = m.Next?.LogCount ?? m.Events.Count;
        return m.Sample.LogCount < end ? m.Events[m.Sample.LogCount] : null;
    }

    /// <summary>A K1 takeaway: the first event after the regroup move is a turnover by the regrouping carrier.</summary>
    private static TurnoverEvent? RegroupTakeaway(Move m)
    {
        if (FirstEventAfter(m) is not TurnoverEvent turnover)
        {
            return null;
        }

        int carrierId = m.Events[m.Sample.LogCount - 1].Context.Placement.PuckCarrierId ?? -1;
        return turnover.LoserId == carrierId ? turnover : null;
    }

    private static int RegroupSteps(ShiftSetup setup) => (int)Math.Floor((setup.Tuning.Time.RegroupSeconds * setup.Tuning.Plays.MaxNodesPerBeat / setup.Tuning.Time.GetSecondsPerAction("skate")) + 1e-9);

    private static int TotalSteps(ShiftSetup setup) => (int)Math.Floor(((setup.Tuning.Time.RegroupSeconds + setup.Tuning.Time.SetupSeconds) * setup.Tuning.Plays.MaxNodesPerBeat / setup.Tuning.Time.GetSecondsPerAction("skate")) + 1e-9);

    [Fact]
    public void RegroupMove_HappensOnlyInTheOwnZone_AndMovesTheRegroupSteps()
    {
        List<Move> regroups = Default.Value.Where(m => m.Sample.Kind == DefenceMoveKind.Regroup).ToList();

        Assert.NotEmpty(regroups);
        foreach (Move m in regroups)
        {
            Assert.Equal(RinkZone.Defensive, m.Setup.Rink.ZoneAtX(AttackerView(m, m.Sample.Puck).X));
            Assert.Equal(Recompute(m.Setup, DefenceOf(m).Plan.System, m.Sample, RegroupSteps(m.Setup)), m.Sample.After);
        }
    }

    [Fact]
    public void SetupAfterAnOwnZoneRegroup_MovesTheRemainingSteps_SoTheTotalIsUnchanged()
    {
        int seen = 0;
        foreach (Move m in Default.Value.Where(m => m.Sample.Kind == DefenceMoveKind.Regroup && m.Next != null))
        {
            // The next move is the play's setup when nothing was logged in between and a play action follows it.
            DefenceMoveSample next = m.Next!;
            if (next.LogCount != m.Sample.LogCount || next.Kind != DefenceMoveKind.Other || next.LogCount >= m.Events.Count || m.Events[next.LogCount].Context.PlayId == null)
            {
                continue;
            }

            int setupSteps = TotalSteps(m.Setup) - RegroupSteps(m.Setup);
            Assert.Equal(Recompute(m.Setup, DefenceOf(m).Plan.System, next, setupSteps), next.After);
            seen++;
        }

        Assert.True(seen > 20, "setups after an own-zone regroup: " + seen);
    }

    [Fact]
    public void FailedRegroupCheck_IsATakeawayByTheNearestCountedForechecker_AfterRegroupTimeOnly()
    {
        int seen = 0;
        foreach (Move m in Default.Value.Where(m => m.Sample.Kind == DefenceMoveKind.Regroup))
        {
            TurnoverEvent? turnover = RegroupTakeaway(m);
            if (turnover == null)
            {
                continue;
            }

            List<Position> forecheckers = CountedForecheckers(m);
            Assert.True(forecheckers.Count > m.Setup.Tuning.Transitions.RegroupForecheckersAllowed, "takeaway with f = " + forecheckers.Count);

            Skater taker = DefenceOf(m).Skaters.Skaters.Single(s => s.Id == turnover.TakerId);
            Position takerPosition = DefenceOf(m).Skaters.SlotOf(taker)!.Value;
            Assert.Equal(ExpectedTaker(m, forecheckers), takerPosition);

            // The taker keeps his node; the regroup costs regroupSeconds and the check has no time of its own.
            GridPoint takerNode = TeamFrame.ToRink(m.Sample.After[takerPosition], m.Sample.Defending, m.Setup.Rink);
            Assert.Equal(m.Setup.Rink.IdOf(takerNode.X, takerNode.Y), turnover.NodeId);
            double gainTime = m.Events[m.Sample.LogCount - 1].Context.Time;
            Assert.Equal(m.Setup.Tuning.Time.RegroupSeconds, turnover.Context.Time - gainTime, 6);
            Assert.Contains(AttackOf(m).Skaters.Skaters, s => s.Id == turnover.LoserId);
            seen++;
        }

        Assert.True(seen > 10, "regroup takeaways: " + seen);
    }

    [Fact]
    public void RegroupWithForecheckersAtTheAllowance_IsNeverChecked()
    {
        int notChecked = 0;
        foreach (Move m in Default.Value.Where(m => m.Sample.Kind == DefenceMoveKind.Regroup))
        {
            if (CountedForecheckers(m).Count <= m.Setup.Tuning.Transitions.RegroupForecheckersAllowed)
            {
                Assert.Null(RegroupTakeaway(m));
                notChecked++;
            }
        }

        Assert.True(notChecked > 0);
    }

    [Fact]
    public void AllowanceZeroChecksOneForechecker_AllowanceThreeNeverChecks()
    {
        List<Move> zero = Moves(DataWith(0), 30).Where(m => m.Sample.Kind == DefenceMoveKind.Regroup).ToList();
        Assert.Contains(zero, m => RegroupTakeaway(m) != null && CountedForecheckers(m).Count == 1);

        List<Move> three = Moves(DataWith(3), 30).Where(m => m.Sample.Kind == DefenceMoveKind.Regroup).ToList();
        Assert.NotEmpty(three);
        Assert.All(three, m => Assert.Null(RegroupTakeaway(m)));
    }

    [Fact]
    public void NeutralZoneRegroup_NeverChecks_AndHasNoRegroupMove()
    {
        Rink rink = ShiftTestData.Data.Rink;
        double regroup = ShiftTestData.Data.Tuning.Time.RegroupSeconds;
        int seen = 0;
        foreach ((string home, string away) in new[] { ("forecheck212", "trap122"), ("trap122", "forecheck212") })
        {
            foreach (string spot in Spots)
            {
                ShiftSetup setup = RegroupSetup(ShiftTestData.Data, spot, home, away);
                for (ulong seed = 1; seed <= 60; seed++)
                {
                    var trace = new Trace();
                    List<SimEvent> events = ShiftSimulator.Run(setup, new Pcg32(seed), trace).Log.Events.ToList();
                    for (int i = 0; i < events.Count - 1; i++)
                    {
                        if (events[i] is not TurnoverEvent gain)
                        {
                            continue;
                        }

                        PlayerPlacement taker = gain.Context.Placement.Find(gain.TakerId)!;
                        GridPoint view = TeamFrame.ToTeamView(new GridPoint(rink.XOf(gain.NodeId), rink.YOf(gain.NodeId)), taker.Team, rink);
                        if (rink.ZoneAtX(view.X) != RinkZone.Neutral)
                        {
                            continue;
                        }

                        // No regroup move and no takeaway right after the neutral-zone regroup.
                        Assert.DoesNotContain(trace.Samples, s => s.Kind == DefenceMoveKind.Regroup && s.LogCount == i + 1);
                        Assert.False(
                            events[i + 1] is TurnoverEvent next && next.LoserId == gain.TakerId && Math.Abs(next.Context.Time - gain.Context.Time - regroup) < 1e-9,
                            "neutral-zone regroup was checked at seed " + seed);
                        seen++;
                    }
                }
            }
        }

        Assert.True(seen > 0);
    }

    [Theory]
    [InlineData(42UL)]
    [InlineData(7UL)]
    public void SameSeed_ProducesIdenticalEventLogs_WithOwnZoneRegroups(ulong seed)
    {
        ShiftSetup setup = RegroupSetup(ShiftTestData.Data, "defensiveLeft", "trap122", "forecheck212");

        Assert.Equal(EventJson.Write(ShiftTestData.Run(seed, setup), 0UL), EventJson.Write(ShiftTestData.Run(seed, setup), 0UL));
    }
}
