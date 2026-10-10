using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Tests.Shift;

/// <summary>
/// M-7 presser rule in whole shifts (D-072): every pass and shot is checked against an independent recomputation from
/// the event's placement snapshot. In a play (event with a play id) the system picks the presser; in a rush or second
/// chance (no play id) the nearest defender does.
/// </summary>
public class PresserPressureTests
{
    private static readonly string[] Spots = { "center", "offensiveLeft", "offensiveRight", "neutralOffensiveLeft", "neutralDefensiveRight", "defensiveLeft" };

    private sealed class Trace : IShotPressureTrace
    {
        public List<ShotPressureSample> Samples { get; } = new();

        public void OnShot(ShotPressureSample sample) => Samples.Add(sample);
    }

    private sealed record Action(SimEvent Event, bool Logged, bool Expected, bool AnyDefenderInRadius, bool InPlay, ShotPressureSample? Shot, PassEvent? PassBefore);

    private static ShiftSetup WithPinch(ShiftSetup s, bool pinch)
    {
        TeamShiftSetup Team(TeamShiftSetup t) => new(
            t.Skaters,
            t.Goalie,
            new ShiftPlan(t.Plan.Plays, t.Plan.System, t.Plan.Transition, new SystemModeInstructions(false, 1, pinch)));
        return new ShiftSetup(s.Rink, s.Tuning, Team(s.Home), Team(s.Away), s.FaceoffSpotId, s.Period, s.StartTime, s.MaxSteps);
    }

    private static ShiftSetup AtSpot(string spot, bool pinch, string home, string away)
    {
        ShiftSetup s = ShiftTestData.Setup(home: home, away: away);
        return WithPinch(new ShiftSetup(s.Rink, s.Tuning, s.Home, s.Away, spot, 1, 0.0, s.MaxSteps), pinch);
    }

    private static GridPoint Point(Rink rink, int nodeId) => new(nodeId / rink.Width, nodeId % rink.Width);

    private static bool PinchSpot(Rink rink, GridPoint puck)
    {
        int offensiveMin = rink.Zones.First(z => z.Zone == RinkZone.Offensive).XMin;
        return (puck.Y == 0 || puck.Y == rink.Width - 1) && puck.X >= offensiveMin && puck.X < rink.OpponentGoal.X;
    }

    /// <summary>The expected M-7 value for the carrier, from the snapshot alone.</summary>
    private static (bool Expected, bool AnyInRadius) Recompute(ShiftSetup setup, EventContext context, int carrierId)
    {
        Rink rink = setup.Rink;
        PlacementSnapshot snapshot = context.Placement;
        PlayerPlacement carrier = snapshot.Find(carrierId);
        TeamSide team = carrier.Team;
        TeamSide defending = TeamSides.Opponent(team);
        TeamShiftSetup defence = defending == TeamSide.Home ? setup.Home : setup.Away;
        var defendersRink = new SortedDictionary<Position, GridPoint>();
        foreach (PlayerPlacement p in snapshot.Players.Where(p => p.Team == defending && !p.IsGoalie))
        {
            Skater skater = defence.Skaters.Skaters.First(s => s.Id == p.PlayerId);
            defendersRink.Add(defence.Skaters.SlotOf(skater)!.Value, Point(rink, p.NodeId));
        }

        GridPoint carrierRink = Point(rink, carrier.NodeId);
        int radius = setup.Tuning.Pressure.UnderPressureNodes;
        bool any = defendersRink.Values.Any(d => GridPoint.Chebyshev(d, carrierRink) <= radius);
        Position presser;
        bool found;
        if (context.PlayId != null)
        {
            GridPoint puck = TeamFrame.ToTeamView(carrierRink, defending, rink);
            var own = defendersRink.ToDictionary(e => e.Key, e => TeamFrame.ToTeamView(e.Value, defending, rink));
            bool pinched = defence.Plan.Instructions.Pinch && PinchSpot(rink, puck);
            found = PresserSelector.FromSystem(defence.Plan.System, rink, puck, PuckState.Controlled, own, pinched, out presser);
        }
        else
        {
            found = PresserSelector.Nearest(defendersRink, carrierRink, out presser);
        }

        bool expected = found && GridPoint.Chebyshev(carrierRink, defendersRink[presser]) <= radius;
        return (expected, any);
    }

    private static bool ReceiverFree(ShiftSetup setup, PassEvent pass)
    {
        Rink rink = setup.Rink;
        PlacementSnapshot snapshot = pass.Context.Placement;
        PlayerPlacement receiver = snapshot.Find(pass.ReceiverId);
        GridPoint node = Point(rink, receiver.NodeId);
        return !snapshot.Players.Any(p => p.Team != receiver.Team && !p.IsGoalie
            && GridPoint.Chebyshev(Point(rink, p.NodeId), node) <= setup.Tuning.Pressure.UnderPressureNodes);
    }

    private static List<Action> Actions(bool pinch, string home, string away)
    {
        var actions = new List<Action>();
        foreach (string spot in Spots.Where(s => ShiftTestData.Data.Rink.FaceoffSpots.Any(f => f.Id == s)))
        {
            ShiftSetup setup = AtSpot(spot, pinch, home, away);
            for (ulong seed = 1; seed <= 120; seed++)
            {
                var trace = new Trace();
                ShiftResult result = ShiftSimulator.Run(setup, new Pcg32(seed), trace);
                int shot = 0;
                PassEvent? lastPass = null;
                foreach (SimEvent e in result.Log.Events)
                {
                    if (e is PassEvent pass)
                    {
                        (bool expected, bool any) = Recompute(setup, pass.Context, pass.PasserId);
                        actions.Add(new Action(pass, pass.UnderPressure, expected, any, pass.Context.PlayId != null, null, null));
                        lastPass = pass;
                    }
                    else if (e is ShotEvent s)
                    {
                        ShotPressureSample sample = trace.Samples[shot++];
                        (bool expected, bool any) = Recompute(setup, s.Context, s.ShooterId);
                        actions.Add(new Action(s, sample.PressureAtShot, expected, any, s.Context.PlayId != null, sample, sample.OffPass ? lastPass : null));
                    }
                    else if (e is not ControlledZoneEntryEvent and not LineChangeEvent)
                    {
                        lastPass = null;
                    }
                }

                Assert.Equal(shot, trace.Samples.Count);
            }
        }

        return actions;
    }

    private static readonly Lazy<List<Action>> Plain = new(() => Actions(false, "forecheck212", "trap122"));

    private static readonly Lazy<List<Action>> Pinched = new(() => Actions(true, "trap122", "forecheck212"));

    public static IEnumerable<object[]> Sets() => new[] { new object[] { false }, new object[] { true } };

    private static List<Action> Set(bool pinch) => pinch ? Pinched.Value : Plain.Value;

    [Theory]
    [MemberData(nameof(Sets))]
    public void PassAndShotPressure_MatchesThePresserRule(bool pinch)
    {
        List<Action> actions = Set(pinch);
        Assert.Contains(actions, a => a.Event is PassEvent && a.InPlay);
        Assert.Contains(actions, a => a.Event is PassEvent && !a.InPlay);
        Assert.Contains(actions, a => a.Event is ShotEvent && a.InPlay);
        Assert.Contains(actions, a => a.Event is ShotEvent && !a.InPlay);
        foreach (Action a in actions)
        {
            Assert.True(a.Expected == a.Logged, a.Event.GetType().Name + " at " + a.Event.Context.Time + ": expected " + a.Expected + ", logged " + a.Logged);
        }
    }

    /// <summary>
    /// A zonal test system: F1 presses one node goal-side of the carrier ([-1, 0]); everyone else holds fixed nodes in
    /// the own zone, where attackers often carry the puck. A fixed-node defender on the carrier's node is not the presser.
    /// </summary>
    private static DefensiveSystem Zonal() => new(
        "zonal",
        "Zonal test system",
        false,
        new[]
        {
            new SystemRule(
                SystemCondition.Always,
                Support.TestPlays.Targets(
                    SystemTarget.PuckOffset(-1, 0),
                    SystemTarget.Node(new GridPoint(2, 1)),
                    SystemTarget.Node(new GridPoint(2, 3)),
                    SystemTarget.Node(new GridPoint(3, 1)),
                    SystemTarget.Node(new GridPoint(3, 3)))),
        });

    private static readonly Lazy<List<Action>> ZonalSet = new(() =>
    {
        var actions = new List<Action>();
        foreach (string spot in Spots.Where(s => ShiftTestData.Data.Rink.FaceoffSpots.Any(f => f.Id == s)))
        {
            ShiftSetup s = AtSpot(spot, false, "forecheck212", "trap122");
            DefensiveSystem zonal = Zonal();
            TeamShiftSetup With(TeamShiftSetup t) => new(t.Skaters, t.Goalie, new ShiftPlan(t.Plan.Plays, zonal, t.Plan.Transition, t.Plan.Instructions));
            var setup = new ShiftSetup(s.Rink, s.Tuning, With(s.Home), With(s.Away), spot, 1, 0.0, s.MaxSteps);
            for (ulong seed = 1; seed <= 120; seed++)
            {
                ShiftResult result = ShiftSimulator.Run(setup, new Pcg32(seed), new Trace());
                foreach (SimEvent e in result.Log.Events)
                {
                    if (e is PassEvent pass)
                    {
                        (bool expected, bool any) = Recompute(setup, pass.Context, pass.PasserId);
                        actions.Add(new Action(pass, pass.UnderPressure, expected, any, pass.Context.PlayId != null, null, null));
                    }
                }
            }
        }

        return actions;
    });

    [Fact]
    public void InAPlay_ADefenderInRadiusWhoIsNotThePresser_GivesNoPressure()
    {
        List<Action> zonal = ZonalSet.Value;
        Assert.All(zonal, a => Assert.Equal(a.Expected, a.Logged));
        Assert.Contains(zonal, a => a.InPlay && a.AnyDefenderInRadius && !a.Logged);
        Assert.All(Plain.Value.Concat(Pinched.Value).Concat(zonal), a => Assert.True(!a.Logged || a.AnyDefenderInRadius));
    }

    [Fact]
    public void WithoutAPlay_NearestDefenderRule_EqualsAnyDefenderInRadius()
    {
        Assert.All(Plain.Value.Concat(Pinched.Value).Where(a => !a.InPlay), a => Assert.Equal(a.AnyDefenderInRadius, a.Logged));
    }

    [Fact]
    public void ShotOffAPass_ReceiverFreeChecksAllOpponents_AndOnlyRemovesPresserPressure()
    {
        List<Action> offPass = Plain.Value.Concat(Pinched.Value).Where(a => a.Shot is { OffPass: true }).ToList();
        Assert.NotEmpty(offPass);
        foreach (Action a in offPass)
        {
            ShiftSetup setup = ShiftTestData.Setup();
            Assert.NotNull(a.PassBefore);
            Assert.Equal(ReceiverFree(setup, a.PassBefore!), a.Shot!.ReceiverFreeAtPass);
            Assert.Equal(a.Shot.PressureAtShot && !a.Shot.ReceiverFreeAtPass, ((ShotEvent)a.Event).UnderPressure);
        }

        Assert.Contains(offPass, a => a.Shot!.PressureAtShot && a.Shot.ReceiverFreeAtPass && !((ShotEvent)a.Event).UnderPressure);
    }

    [Fact]
    public void SameSeed_GivesTheSamePressureFlags()
    {
        ShiftSetup setup = AtSpot("offensiveLeft", true, "forecheck212", "trap122");
        for (ulong seed = 1; seed <= 30; seed++)
        {
            string Flags() => string.Join(",", ShiftSimulator.Run(setup, new Pcg32(seed)).Log.Events.Select(e => e switch
            {
                PassEvent p => "p" + p.PasserId + ":" + p.UnderPressure + ":" + p.Succeeded,
                ShotEvent s => "s" + s.ShooterId + ":" + s.UnderPressure + ":" + s.Outcome,
                _ => e.GetType().Name,
            }));
            Assert.Equal(Flags(), Flags());
        }
    }
}
