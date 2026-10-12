using HockeyCoach.Replay.Core;
using HockeyCoach.Replay.Tests.Support;

namespace HockeyCoach.Replay.Tests;

/// <summary>Puck motion of the presentation layer (D-088, Q-079, data-schema.md, replay.json, Kiekko) on the seed 42 match.</summary>
[Collection(Seed42Collection.Name)]
public sealed class PuckMotionTests
{
    private const double Eps = 1e-9;
    private readonly Seed42Match _m;

    public PuckMotionTests(Seed42Match m)
    {
        _m = m;
    }

    private PresentationTimeline P => _m.Presentation;

    private PuckMotion Puck => P.Motion.Puck;

    private PuckSettings Cfg => _m.Config.Puck;

    private IEnumerable<(PuckPlan Plan, TimelineSegment Segment)> Plans =>
        Puck.Plans.Select(plan => (plan, P.Timeline.Segments[plan.SegmentIndex]));

    [Fact]
    public void Flights_StartAtTheWindow_FromThePreviousPuckState_AndLegsFollowEachOther()
    {
        foreach ((PuckPlan plan, TimelineSegment seg) in Plans)
        {
            Assert.Equal(P.Segments[plan.SegmentIndex].WindowStart, plan.WindowStart);
            if (plan.Legs.Count == 0)
            {
                continue;
            }

            Assert.Equal(plan.WindowStart, plan.Legs[0].Start);
            if (seg.Type != ReplayEventType.Faceoff)
            {
                Assert.Equal(plan.Start, plan.Legs[0].From);
            }

            for (int k = 1; k < plan.Legs.Count; k++)
            {
                Assert.Equal(plan.Legs[k - 1].End, plan.Legs[k].Start);
                Assert.Equal(plan.Legs[k - 1].To, plan.Legs[k].From);
            }
        }
    }

    [Fact]
    public void LegSpeeds_AreTheConfiguredSpeedsOfTheEventType()
    {
        int homing = 0;
        int off = 0;
        foreach ((PuckPlan plan, TimelineSegment seg) in Plans)
        {
            if (plan.Legs.Count == 0 || (plan.InNet && seg.Type != ReplayEventType.Shot))
            {
                continue;
            }

            PuckLeg first = plan.Legs[0];
            switch (seg.Type)
            {
                case ReplayEventType.Pass:
                    double straight = RinkPoint.Distance(first.From, first.To);
                    Assert.Equal(straight >= Cfg.LongPassMinMetres ? Cfg.LongPassSpeed : Cfg.PassSpeed, first.Speed);
                    break;
                case ReplayEventType.Shot:
                    Assert.Equal(Cfg.ShotSpeed, first.Speed);
                    if (plan.Legs.Count > 1)
                    {
                        double second = seg.Event.Outcome == "Blocked" ? Cfg.BlockDeflectSpeed : Cfg.ReboundSpeed;
                        Assert.Equal(second, plan.Legs[1].Speed);
                    }

                    break;
                case ReplayEventType.DumpIn:
                    Assert.Equal(Cfg.DumpInSpeed, first.Speed);
                    break;
                case ReplayEventType.Turnover:
                case ReplayEventType.PuckBattle:
                    Assert.Equal(Cfg.LoosePuckSpeed, first.Speed);
                    break;
                case ReplayEventType.Faceoff:
                    Assert.Equal(0, first.Speed);
                    Assert.Equal(Cfg.FaceoffDrawDelaySeconds, first.End - first.Start, 9);
                    Assert.Equal(first.From, first.To);
                    Assert.Equal(seg.FaceoffSpot!.Position, first.From);
                    Assert.Equal(Cfg.FaceoffDrawSpeed, plan.Legs[1].Speed);
                    break;
            }

            foreach (PuckLeg leg in plan.Legs.Where(l => l.Speed > 0))
            {
                // Each leg lasts its straight length (to the end point, or the homing target at arrival) over its speed.
                RinkPoint end = leg.TargetId.HasValue ? Puck.Carried(leg.TargetId.Value, leg.End, null) : leg.To;
                double flown = RinkPoint.Distance(leg.From, end) / (leg.End - leg.Start);
                Assert.True(leg.End > leg.Start || RinkPoint.Distance(leg.From, end) < 1e-6);
                if (leg.End > leg.Start && !leg.TargetId.HasValue)
                {
                    Assert.InRange(flown, leg.Speed * (1 - 1e-9), leg.Speed * (1 + 1e-9));
                }
                else if (leg.End > leg.Start)
                {
                    homing++;
                    if (Math.Abs(flown - leg.Speed) > leg.Speed * 0.01)
                    {
                        off++;
                    }
                }
            }
        }

        // A homing leg reaches its target exactly at the root of |from − target(t)| = speed × flight. When the target
        // outruns the puck or jumps with separation (Q-084), there is no root and the leg flies to the closest approach
        // at another average speed; that stays rare.
        Assert.True(off * 25 < homing, off + " of " + homing + " homing legs off their speed by more than 1 %");
    }

    [Fact]
    public void Criterion3_PassesAndShots_EndAtThePlacement_ExceptGoalsInTheNet()
    {
        RinkMapping map = P.Timeline.Mapping;
        int checkedFlights = 0;
        foreach ((PuckPlan plan, TimelineSegment seg) in Plans.Where(x => x.Segment.Type is ReplayEventType.Pass or ReplayEventType.Shot))
        {
            int index = plan.SegmentIndex;
            int at = P.SegmentIndexAt(plan.FlightEnd);
            RinkPoint end = Puck.PositionAt(at, plan.FlightEnd, P.Motion.Skaters.Sample(at, plan.FlightEnd), out int? carrier);
            if (seg.Event.Outcome == "Goal")
            {
                ReplayTeam conceding = seg.ScoringTeam == ReplayTeam.Home ? ReplayTeam.Away : ReplayTeam.Home;
                RinkPoint net = PuckGeometry.NetInside(map, conceding, _m.Config.Goal.NetInsideMetres);
                Assert.Equal(net, plan.EndPoint);
                Assert.Equal(net, plan.Legs[^1].To);
                Assert.True(plan.InNet);
            }
            else if (seg.Event.Placement.PuckCarrierId is int holder)
            {
                Assert.Equal(holder, plan.EndCarrierId);
                Assert.True(RinkPoint.Distance(Puck.Carried(holder, plan.FlightEnd, null), plan.Legs[^1].TargetId.HasValue ? Puck.Carried(holder, plan.FlightEnd, null) : plan.Legs[^1].To) < 1e-6);
                SegmentPlayer? h = seg.Players.FirstOrDefault(p => p.Id == holder);
                Assert.NotNull(h);
                Assert.Equal(seg.Event.Placement.PuckNodeId, h!.NodeId);
            }
            else
            {
                Assert.Null(plan.EndCarrierId);
                Assert.Equal(map.NodePosition(seg.Event.Placement.PuckNodeId), plan.EndPoint);
                Assert.Equal(plan.EndPoint, plan.Legs[^1].To);
                if (at == index)
                {
                    Assert.Equal(plan.EndPoint, end);
                }
            }

            checkedFlights++;
        }

        Assert.Equal(_m.Match.Events.Count(e => e.Type is "Pass" or "Shot"), checkedFlights);
    }

    [Fact]
    public void Criterion5_ShotsGoThroughTheGoalieOrBlocker_OrBesideTheNetWhenMissed()
    {
        RinkMapping map = P.Timeline.Mapping;
        int saved = 0;
        int blocked = 0;
        int missed = 0;
        foreach ((PuckPlan plan, TimelineSegment seg) in Plans.Where(x => x.Segment.Type == ReplayEventType.Shot && x.Segment.Event.Outcome != "Goal"))
        {
            if (plan.InNet)
            {
                continue;
            }

            ReplayTeam shooter = ReplayTeams.Parse(P.Timeline.FindPlayer(seg.Event.ShooterId!.Value)!.Team);
            ReplayTeam target = shooter == ReplayTeam.Home ? ReplayTeam.Away : ReplayTeam.Home;
            PuckLeg shot = plan.Legs[0];
            switch (seg.Event.Outcome)
            {
                case "Saved":
                    Assert.Equal(ReplayMotion.GoaliePosition(map, _m.Config.Goalies, target, map.NetOf(target).Y), shot.To);
                    saved++;
                    break;
                case "Blocked":
                    int at = P.SegmentIndexAt(shot.End);
                    SkaterPose blocker = P.Motion.Skaters.Sample(at, shot.End).Single(p => p.Id == seg.Event.StoppedById);
                    Assert.Equal(blocker.Position, shot.To);
                    Assert.NotEqual(shooter, blocker.Team);
                    blocked++;
                    break;
                case "Missed":
                    Assert.Equal(target == ReplayTeam.Away ? map.AwayGoalLineX : map.HomeGoalLineX, shot.To.X, 9);
                    Assert.Equal((PuckGeometry.NetWidthMetres / 2) + Cfg.MissWideMetres, Math.Abs(shot.To.Y - map.NetOf(target).Y), 9);
                    missed++;
                    break;
            }
        }

        Assert.True(saved > 10 && blocked > 5 && missed > 10, saved + " / " + blocked + " / " + missed);
    }

    [Fact]
    public void GoalPuck_StaysInTheNet_UntilTheNextFaceoffsWindow()
    {
        IReadOnlyList<SweepFrame> frames = MotionSweep.Of(P).Frames;
        int goals = 0;
        foreach (PuckPlan goal in Puck.Plans.Where(pl => pl.InNet && P.Timeline.Segments[pl.SegmentIndex].Type == ReplayEventType.Shot))
        {
            PuckPlan faceoff = Puck.Plans.First(pl => pl.SegmentIndex > goal.SegmentIndex && P.Timeline.Segments[pl.SegmentIndex].Type == ReplayEventType.Faceoff);
            int inNet = 0;
            foreach (SweepFrame f in frames.Where(f => f.Time >= goal.FlightEnd && f.Time < faceoff.WindowStart))
            {
                Assert.Equal(goal.EndPoint, f.Sample.Puck);
                Assert.Null(f.Sample.PuckCarrierId);
                inNet++;
            }

            Assert.True(inNet > 60, "the goal puck should be visible in the net for more than a second, frames " + inNet);
            SweepFrame after = frames.First(f => f.Time >= faceoff.WindowStart);
            Assert.Equal(P.Timeline.Segments[faceoff.SegmentIndex].FaceoffSpot!.Position, after.Sample.Puck);
            goals++;
        }

        Assert.Equal(P.Timeline.Goals.Count, goals);
    }

    [Fact]
    public void CarriedPuck_IsTheLeadInFrontOfTheCarrier()
    {
        int carried = 0;
        foreach (SweepFrame f in MotionSweep.Of(P).Frames)
        {
            if (f.Sample.PuckCarrierId is not int id)
            {
                continue;
            }

            PlayerSample carrier = f.Sample.Players.Concat(f.Sample.Leaving).Single(p => p.Id == id);
            Assert.Equal(Cfg.CarriedLeadMetres, RinkPoint.Distance(carrier.Position, f.Sample.Puck), 9);
            carried++;
        }

        Assert.True(carried > 10000);
    }

    [Fact]
    public void Puck_DoesNotJump_ExceptToTheFaceoffSpot_OrWithItsCarrier()
    {
        // Per frame the puck moves at most at the fastest configured speed, plus the largest drawn move of a player in that
        // frame (its carrier or homing target, which can flip with separation, Q-084) and a turn of the carried lead; it
        // jumps only to the faceoff spot at the faceoff's window start (Q-079).
        double fastest = new[] { Cfg.PassSpeed, Cfg.LongPassSpeed, Cfg.ShotSpeed, Cfg.DumpInSpeed, Cfg.ReboundSpeed, Cfg.BlockDeflectSpeed, Cfg.LoosePuckSpeed, Cfg.FaceoffDrawSpeed }.Max();
        double flightStep = fastest * MotionSweep.Frame * 1.05;
        var faceoffStarts = Puck.Plans.Where(pl => P.Timeline.Segments[pl.SegmentIndex].Type == ReplayEventType.Faceoff).Select(pl => pl.WindowStart).ToList();
        IReadOnlyList<SweepFrame> frames = MotionSweep.Of(P).Frames;
        for (int k = 1; k < frames.Count; k++)
        {
            SweepFrame a = frames[k - 1];
            SweepFrame b = frames[k];
            if (P.Timeline.Segments[a.Segment].Period != P.Timeline.Segments[b.Segment].Period || faceoffStarts.Any(t => t > a.Time && t <= b.Time))
            {
                continue;
            }

            double moved = RinkPoint.Distance(a.Sample.Puck, b.Sample.Puck);
            double carrierMove = 0;
            foreach (PlayerSample pb in b.Sample.Players.Concat(b.Sample.Leaving))
            {
                PlayerSample? pa = a.Sample.Players.Concat(a.Sample.Leaving).FirstOrDefault(p => p.Id == pb.Id);
                if (pa != null)
                {
                    carrierMove = Math.Max(carrierMove, RinkPoint.Distance(pa.Position, pb.Position));
                }
            }

            double allowed = flightStep + carrierMove + (2 * Cfg.CarriedLeadMetres) + 1e-6;
            Assert.True(moved <= allowed, "puck jumped " + moved + " m at " + b.Time + " (segment " + b.Segment + " " + P.Timeline.Segments[b.Segment].Type + ")");
        }
    }
}
