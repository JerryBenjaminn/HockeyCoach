using HockeyCoach.Replay.Core;
using HockeyCoach.Replay.Tests.Support;
using Newtonsoft.Json.Linq;

namespace HockeyCoach.Replay.Tests;

/// <summary>Skater and goalie motion of the presentation layer (D-088, data-schema.md, replay.json, Luistelijat, Maalivahdit).</summary>
[Collection(Seed42Collection.Name)]
public sealed class SkaterMotionTests
{
    private const double Eps = 1e-9;
    private readonly Seed42Match _m;

    public SkaterMotionTests(Seed42Match m)
    {
        _m = m;
    }

    private PresentationTimeline P => _m.Presentation;

    private SkaterMotion Skaters => P.Motion.Skaters;

    private SkaterSettings Sk => _m.Config.Skaters;

    private MotionSweep Sweep => MotionSweep.Of(P);

    [Fact]
    public void Offset_FollowsTheSplitMix64Formula_AndStaysInsideTheRadius()
    {
        ulong seed = _m.Match.Seed;
        foreach (int id in Skaters.Tracks.Keys)
        {
            RinkPoint o = Skaters.OffsetOf(id);
            double u1 = Reference(seed, id, 1);
            double u2 = Reference(seed, id, 2);
            double r = Sk.NodeOffsetRadiusMetres * Math.Sqrt(u1);
            Assert.Equal(r * Math.Cos(2 * Math.PI * u2), o.X, 12);
            Assert.Equal(r * Math.Sin(2 * Math.PI * u2), o.Y, 12);
            Assert.True(Math.Sqrt((o.X * o.X) + (o.Y * o.Y)) <= Sk.NodeOffsetRadiusMetres + Eps);
            Assert.Equal(2 * Math.PI * Reference(seed, id, 3), SkaterOffsets.Phase(seed, id), 12);
        }

        Assert.Equal(Skaters.Tracks.Count, Skaters.Tracks.Keys.Select(id => Skaters.OffsetOf(id)).Distinct().Count());
        Assert.NotEqual(SkaterOffsets.Offset(1, 5, 1), SkaterOffsets.Offset(2, 5, 1));
        Assert.Equal(SkaterOffsets.Offset(42, 5, 1), SkaterOffsets.Offset(42, 5, 1));
    }

    [Fact]
    public void EveryTarget_IsTheNodePlusOffset_OrABenchDoor()
    {
        RinkMapping map = P.Timeline.Mapping;
        var gates = new[] { ReplayTeam.Home, ReplayTeam.Away }
            .SelectMany(t => new[] { PresentationTimeline.BenchExit(map, _m.Config.Bench, t), PresentationTimeline.BenchEntry(map, _m.Config.Bench, t) })
            .ToHashSet();
        var nodes = Enumerable.Range(0, map.NodeCount).Select(map.NodePosition).ToList();
        foreach (SkaterTrack track in Skaters.Tracks.Values)
        {
            RinkPoint o = Skaters.OffsetOf(track.Id);
            foreach (RinkPoint point in track.Stints.SelectMany(s => s.Chains).SelectMany(c => c.IsApproach ? c.Points.Skip(1) : c.Points))
            {
                bool onNode = nodes.Any(n => Math.Abs(n.X + o.X - point.X) < 1e-9 && Math.Abs(n.Y + o.Y - point.Y) < 1e-9);
                Assert.True(onNode || gates.Contains(point), "player " + track.Id + " target " + point);
            }
        }
    }

    [Fact]
    public void Arrivals_FollowTheLatenessRule_AndThePathIsOnTheTargetAtEachArrival()
    {
        foreach (SkaterTrack track in Skaters.Tracks.Values)
        {
            foreach (TrackChain c in track.Stints.SelectMany(s => s.Chains).Where(c => !c.IsApproach))
            {
                for (int k = 0; k < c.Points.Count; k++)
                {
                    double expected = k == 0 ? c.Targets[0] : Math.Max(c.Targets[k], c.Arrivals[k - 1] + (c.Legs[k - 1].Length / Sk.TopSpeed));
                    Assert.Equal(expected, c.Arrivals[k], 9);
                    RinkPoint at = c.StateAt(c.Arrivals[k]).Position;
                    Assert.True(RinkPoint.Distance(c.Points[k], at) < 1e-6, "player " + track.Id + " off target at " + c.Arrivals[k]);
                }
            }
        }
    }

    [Fact]
    public void ActorsOfAGap_AreOnTargetAtTheirArrival_AndWhenOnTimeAtTheTargetTime()
    {
        int onTime = 0;
        for (int i = 1; i < P.Segments.Count; i++)
        {
            SegmentTiming tm = P.Segments[i];
            TimelineSegment seg = P.Timeline.Segments[i];
            if (!(tm.Duration > 0))
            {
                continue;
            }

            double target = SkaterArrivals.TargetTime(tm, Sk);
            foreach (int id in PresentationTimeline.Actors(seg.Event, P.Timeline.Segments[i - 1].Event))
            {
                SegmentPlayer? sp = seg.Players.FirstOrDefault(x => x.Id == id && !x.IsGoalie);
                if (sp == null)
                {
                    continue;
                }

                TrackChain? chain = Skaters.Tracks[id].Stints.SelectMany(s => s.Chains).FirstOrDefault(c => c.Targets.Any(x => Math.Abs(x - target) < Eps));
                if (chain == null)
                {
                    continue;
                }

                int k = chain.Targets.ToList().FindIndex(x => Math.Abs(x - target) < Eps);
                Assert.Equal(Skaters.Target(id, sp.NodeId), chain.Points[k]);
                if (chain.Arrivals[k] <= target + Eps)
                {
                    Assert.True(RinkPoint.Distance(Skaters.Target(id, sp.NodeId), chain.StateAt(target).Position) < 1e-6);
                    onTime++;
                }
            }
        }

        Assert.True(onTime > 1000);
    }

    [Fact]
    public void PathSpeed_NeverExceedsTopSpeed_OutsideFaceoffApproaches()
    {
        double step = (Sk.TopSpeed * MotionSweep.Frame) + 1e-3;
        var last = new Dictionary<int, (RinkPoint Position, int Segment)>();
        foreach (SweepFrame f in Sweep.Frames)
        {
            var now = new Dictionary<int, (RinkPoint, int)>();
            foreach (SkaterPose pose in f.Skaters)
            {
                Assert.True(pose.Path.Speed <= Sk.TopSpeed + Eps || pose.Path.IsApproach, "speed " + pose.Path.Speed + " of " + pose.Id + " at " + f.Time);
                if (last.TryGetValue(pose.Id, out var before) && !pose.Path.IsApproach && SamePeriod(before.Segment, f.Segment))
                {
                    double moved = RinkPoint.Distance(before.Position, pose.Path.Position);
                    Assert.True(moved <= step || ApproachBetween(f.Time - MotionSweep.Frame, f.Time), "player " + pose.Id + " moved " + moved + " m at " + f.Time);
                }

                now[pose.Id] = (pose.Path.Position, f.Segment);
            }

            last = now;
        }
    }

    [Fact]
    public void FaceoffApproach_StartsWhereThePlayerIs_AndArrivesExactlyAtTheFaceoffTarget()
    {
        int approaches = 0;
        foreach (SkaterTrack track in Skaters.Tracks.Values)
        {
            foreach (SkaterStint stint in track.Stints)
            {
                for (int c = 0; c < stint.Chains.Count; c++)
                {
                    TrackChain chain = stint.Chains[c];
                    if (!chain.IsApproach)
                    {
                        continue;
                    }

                    approaches++;
                    if (c > 0)
                    {
                        RinkPoint before = stint.Chains[c - 1].StateAt(chain.Start).Position;
                        Assert.True(RinkPoint.Distance(before, chain.Points[0]) < 1e-9, "approach of " + track.Id + " teleports");
                    }

                    Assert.Equal(0, chain.Profiles[0].SpeedAt(0), 9);
                    Assert.Equal(0, chain.Profiles[0].SpeedAt(chain.Profiles[0].Duration), 9);
                    Assert.Equal(chain.Points[1], chain.StateAt(chain.End).Position);
                    // Outgoing players of a stoppage change skate to their bench in the change phase (D-089 A1), everybody
                    // else to the faceoff in the approach after it.
                    SegmentTiming? change = P.Segments.FirstOrDefault(s => s.ChangeSeconds > 0 && Math.Abs(s.Start - chain.Start) < Eps && Math.Abs(s.ApproachStart - chain.End) < Eps);
                    SegmentTiming? tm = P.Segments.FirstOrDefault(s => Math.Abs(s.ApproachStart - chain.Start) < Eps && s.ApproachSeconds > 0);
                    Assert.True(change != null || (tm != null && Math.Abs(tm.GapStart - chain.End) < 1e-9), "approach of " + track.Id + " at " + chain.Start);
                }
            }
        }

        Assert.True(approaches > 100);
    }

    [Fact]
    public void DrawnPositions_OnlyJumpWhereSeparationFlips_WithinTheStatedTolerance()
    {
        // Separation (data-schema.md, Erottelu) is stateless: when two skaters' paths cross, the push between them flips
        // side, so a drawn position can move by up to separationMin per pair and iteration in one frame (Q-084). The path
        // itself never jumps (PathSpeed test). Tolerance per frame: the path step + the largest idle step
        // (2 × 2π f × amplitude × frame) + separationMin × separationIterations; such frames stay rare.
        double idleStep = 2 * 2 * Math.PI * Sk.IdleFrequencyHz * Math.Max(1, Sk.IdleYFrequencyRatio) * Sk.IdleAmplitudeMetres * MotionSweep.Frame;
        double separation = Sk.SeparationMinMetres * Sk.SeparationIterations;
        var last = new Dictionary<int, (RinkPoint Position, RinkPoint Path, int Segment)>();
        long moves = 0;
        long big = 0;
        foreach (SweepFrame f in Sweep.Frames)
        {
            var now = new Dictionary<int, (RinkPoint, RinkPoint, int)>();
            foreach (SkaterPose pose in f.Skaters)
            {
                if (last.TryGetValue(pose.Id, out var before) && SamePeriod(before.Segment, f.Segment))
                {
                    double moved = RinkPoint.Distance(before.Position, pose.Position);
                    double pathMoved = RinkPoint.Distance(before.Path, pose.Path.Position);
                    Assert.True(moved <= pathMoved + idleStep + separation + 1e-9, "player " + pose.Id + " jumped " + moved + " m at " + f.Time);
                    moves++;
                    if (moved > 0.5)
                    {
                        big++;
                    }
                }

                now[pose.Id] = (pose.Position, pose.Path.Position, f.Segment);
            }

            last = now;
        }

        Assert.True(big < moves / 100, big + " of " + moves + " frame moves exceed 0.5 m");
    }

    [Fact]
    public void Separation_KeepsPairsApart_WithTheMeasuredResidual()
    {
        // Three passes over the pairs do not resolve every crowd (and the clamp at the boards can push players back
        // together), so the spec's minimum is a goal, not a guarantee (Q-084). Changing players are left out of the
        // separation (D-089 A1), so only pairs of settled players count. Asserted: such pairs closer than
        // separationMin are under 1 % of all their samples.
        long pairs = 0;
        long close = 0;
        foreach (SweepFrame f in Sweep.Frames)
        {
            for (int a = 0; a < f.Skaters.Length; a++)
            {
                for (int b = a + 1; b < f.Skaters.Length; b++)
                {
                    if (f.Skaters[a].IsChanging || f.Skaters[b].IsChanging)
                    {
                        continue;
                    }

                    pairs++;
                    if (RinkPoint.Distance(f.Skaters[a].Position, f.Skaters[b].Position) < Sk.SeparationMinMetres - 1e-6)
                    {
                        close++;
                    }
                }
            }
        }

        Assert.True(close < pairs / 100, close + " of " + pairs);
    }

    [Fact]
    public void Separate_PushesAClosePairApartSymmetrically_AndCoincidentPairsAlongTheSmallerIdsAngle()
    {
        var x = new[] { 10.0, 11.0, 30.0 };
        var y = new[] { 5.0, 5.0, 5.0 };
        int[] ids = Skaters.Tracks.Keys.Take(3).ToArray();
        Skaters.Separate(x, y, ids, new[] { 0, 1, 2 }, 2, 1);
        Assert.Equal(9.5, x[0], 12);
        Assert.Equal(11.5, x[1], 12);
        Assert.Equal(30, x[2]);

        var cx = new[] { 10.0, 10.0 };
        var cy = new[] { 5.0, 5.0 };
        Skaters.Separate(cx, cy, ids.Take(2).ToArray(), new[] { 0, 1 }, 2, 1);
        double a = SkaterOffsets.Angle(_m.Match.Seed, ids[0]);
        Assert.Equal(10 + Math.Cos(a), cx[0], 12);
        Assert.Equal(5 + Math.Sin(a), cy[0], 12);
        Assert.Equal(10 - Math.Cos(a), cx[1], 12);
    }

    [Fact]
    public void EveryFrame_ShowsTheTwelve_InsideTheRink_AndGoaliesInTheirCreases()
    {
        RinkMapping map = P.Timeline.Mapping;
        GoalieSettings g = _m.Config.Goalies;
        foreach (SweepFrame f in Sweep.Frames)
        {
            MotionSample s = f.Sample;
            Assert.Equal(12, s.Players.Count);
            Assert.Equal(P.Timeline.Segments[f.Segment].Players.Select(p => p.Id), s.Players.Select(p => p.Id));
            foreach (PlayerSample p in s.Players.Concat(s.Leaving))
            {
                Assert.InRange(p.Position.X, 0, map.LengthMetres);
                Assert.InRange(p.Position.Y, 0, map.WidthMetres);
                if (p.IsGoalie)
                {
                    double x = p.Team == ReplayTeam.Home ? map.HomeGoalLineX + g.DepthMetres : map.AwayGoalLineX - g.DepthMetres;
                    Assert.Equal(x, p.Position.X, 9);
                    Assert.True(Math.Abs(p.Position.Y - map.NetOf(p.Team).Y) <= g.LateralMaxMetres + Eps);
                }
            }

            Assert.DoesNotContain(s.Leaving, l => s.Players.Any(p => p.Id == l.Id));
        }
    }

    [Fact]
    public void GoaliePosition_TracksThePuckLaterally_WithinItsLimit()
    {
        RinkMapping map = P.Timeline.Mapping;
        GoalieSettings g = _m.Config.Goalies;
        RinkPoint net = map.NetOf(ReplayTeam.Away);
        RinkPoint near = ReplayMotion.GoaliePosition(map, g, ReplayTeam.Away, net.Y + 2);
        Assert.Equal(net.Y + (g.LateralTrackFactor * 2), near.Y, 12);
        Assert.Equal(map.AwayGoalLineX - g.DepthMetres, near.X, 12);
        RinkPoint far = ReplayMotion.GoaliePosition(map, g, ReplayTeam.Away, net.Y - 100);
        Assert.Equal(net.Y - g.LateralMaxMetres, far.Y, 12);
        Assert.Equal(map.HomeGoalLineX + g.DepthMetres, ReplayMotion.GoaliePosition(map, g, ReplayTeam.Home, net.Y).X, 12);
    }

    [Fact]
    public void LineChanges_LeavingPlayersSkateToTheirBenchExit_AndIncomingStartAtTheEntry()
    {
        RinkMapping map = P.Timeline.Mapping;
        int leaves = 0;
        int entries = 0;
        foreach (SkaterTrack track in Skaters.Tracks.Values)
        {
            RinkPoint exit = PresentationTimeline.BenchExit(map, _m.Config.Bench, track.Team);
            RinkPoint entry = PresentationTimeline.BenchEntry(map, _m.Config.Bench, track.Team);
            foreach (SkaterStint stint in track.Stints)
            {
                if (!double.IsPositiveInfinity(stint.Vanish) && stint.Chains[^1].Points[^1] == exit)
                {
                    Assert.True(RinkPoint.Distance(exit, stint.StateAt(stint.Vanish - 1e-9).Position) < 1e-6);
                    leaves++;
                }

                if (stint.Chains[0].Points[0] == entry)
                {
                    Assert.Equal(entry, stint.EntryPoint);
                    entries++;
                }
            }
        }

        int changes = P.Timeline.Segments.Where(s => s.Type == ReplayEventType.LineChange).Sum(s => s.Leaving.Count);
        Assert.True(leaves > changes / 2, leaves + " gate exits for " + changes + " outgoing");
        Assert.True(entries > changes / 2, entries + " gate entries for " + changes + " incoming");
    }

    [Fact]
    public void IncomingPlayers_EnterOnlyWhenTheirOutgoingPartnerIsNearTheBench()
    {
        RinkMapping map = P.Timeline.Mapping;
        double within = _m.Config.Bench.EnterWithinMetres;
        int checkedEntries = 0;
        for (int i = 0; i < P.Segments.Count; i++)
        {
            TimelineSegment seg = P.Timeline.Segments[i];
            if (seg.Type != ReplayEventType.LineChange || (P.Segments[i].Flags & SegmentTimingFlags.PeriodStart) != 0)
            {
                continue;
            }

            List<int> outgoing = seg.Event.OutgoingIds!.OrderBy(x => x).ToList();
            List<int> incoming = seg.Event.IncomingIds!.OrderBy(x => x).ToList();
            for (int k = 0; k < Math.Min(outgoing.Count, incoming.Count); k++)
            {
                SkaterStint? enter = Skaters.Tracks[incoming[k]].Stints.FirstOrDefault(s => s.EntryPoint.HasValue && s.Appear >= P.Segments[i].Start - 1e-9);
                SkaterStint? leave = Skaters.Tracks[outgoing[k]].Stints.FirstOrDefault(s => s.Appear < P.Segments[i].Start && s.Vanish >= P.Segments[i].Start - 1e-9);
                if (enter == null || leave == null || enter.Appear > leave.Vanish + 1e-9)
                {
                    continue;
                }

                RinkPoint exit = PresentationTimeline.BenchExit(map, _m.Config.Bench, Skaters.Tracks[outgoing[k]].Team);
                double gap = RinkPoint.Distance(leave.StateAt(Math.Min(enter.Appear, leave.Vanish - 1e-9)).Position, exit);
                Assert.True(gap <= within + 1e-3, "player " + incoming[k] + " entered while " + outgoing[k] + " was " + gap + " m from the bench");
                checkedEntries++;
            }
        }

        Assert.True(checkedEntries > 100, "entries checked " + checkedEntries);
    }

    [Fact]
    public void StoppageChanges_HappenBeforeTheApproach_OutgoingLeaveByItsStart()
    {
        int changed = 0;
        foreach (SegmentTiming tm in P.Segments.Where(s => s.ChangeSeconds > 0))
        {
            foreach (SkaterTrack track in Skaters.Tracks.Values)
            {
                foreach (SkaterStint stint in track.Stints)
                {
                    if (Math.Abs(stint.Vanish - tm.ApproachStart) < 1e-9)
                    {
                        changed++;
                    }

                    if (stint.EntryPoint.HasValue && stint.Appear >= tm.Start - 1e-9 && stint.Appear <= tm.ApproachStart + 1e-9)
                    {
                        // Entered during the change phase: stands at the entry until the approach starts.
                        Assert.Equal(stint.EntryPoint.Value, stint.StateAt(tm.ApproachStart).Position);
                        changed++;
                    }
                }
            }
        }

        Assert.True(changed > 50, "changes " + changed);
    }

    [Fact]
    public void EveryFrame_DrawsAtLeastTwelve_AndChangingPlayersAreNotSeparated()
    {
        SkaterSettings sk = Sk;
        double w = 2 * Math.PI * sk.IdleFrequencyHz;
        RinkMapping map = P.Timeline.Mapping;
        foreach (SweepFrame f in Sweep.Frames)
        {
            int drawn = f.Sample.Players.Count(p => !p.IsWaiting) + f.Sample.Leaving.Count;
            Assert.True(drawn >= 12, drawn + " players drawn at " + f.Time);
            foreach (SkaterPose pose in f.Skaters.Where(x => x.IsChanging))
            {
                double amplitude = pose.IsWaiting ? 0 : sk.IdleAmplitudeMetres * (1 - Math.Min(1, pose.Path.Speed / sk.TopSpeed));
                double phi = SkaterOffsets.Phase(_m.Match.Seed, pose.Id);
                double x = Math.Max(0, Math.Min(map.LengthMetres, pose.Path.Position.X + (amplitude * Math.Sin((w * f.Time) + phi))));
                double y = Math.Max(0, Math.Min(map.WidthMetres, pose.Path.Position.Y + (amplitude * Math.Sin((w * sk.IdleYFrequencyRatio * f.Time) + (2 * phi)))));
                Assert.Equal(x, pose.Position.X, 9);
                Assert.Equal(y, pose.Position.Y, 9);
            }
        }
    }

    [Fact]
    public void IdleMotion_IsAddedToAStandingSkater_WhenNobodyIsClose()
    {
        PresentationTimeline p = new SyntheticMatch()
            .Add("Faceoff", 1, 0, 27, 1, SyntheticMatch.OnIce(27, 2, 52, 22), new JObject { ["homeCentreId"] = 1, ["awayCentreId"] = 101 })
            .Add("Pass", 1, 30, 27, 1, SyntheticMatch.OnIce(27, 2, 52, 22), new JObject { ["passerId"] = 1, ["receiverId"] = 1, ["succeeded"] = true })
            .Present();
        SkaterSettings sk = p.Config.Skaters;
        double t = p.Segments[1].Start + 1.234;
        SkaterPose pose = p.Motion.Skaters.Sample(1, t).Single(x => x.Id == 1);
        Assert.Equal(0, pose.Path.Speed);
        double phi = SkaterOffsets.Phase(p.Timeline.Match.Seed, 1);
        double w = 2 * Math.PI * sk.IdleFrequencyHz;
        Assert.Equal(pose.Path.Position.X + (sk.IdleAmplitudeMetres * Math.Sin((w * t) + phi)), pose.Position.X, 12);
        Assert.Equal(pose.Path.Position.Y + (sk.IdleAmplitudeMetres * Math.Sin((w * sk.IdleYFrequencyRatio * t) + (2 * phi))), pose.Position.Y, 12);
    }

    [Fact]
    public void Motion_IsDeterministic_AcrossBuilds()
    {
        PresentationTimeline again = PresentationTimeline.Build(TimelineBuilder.Build(ReplayJsonReader.Read(_m.Json)), ReplayConfigFile.Read());
        var times = Enumerable.Range(0, 400).Select(k => k * P.Duration / 400).ToList();
        foreach (double t in times)
        {
            int i = P.SegmentIndexAt(t);
            MotionSample a = P.Motion.Sample(i, t);
            MotionSample b = again.Motion.Sample(i, t);
            Assert.Equal(a.Players.Select(x => (x.Id, x.Position)), b.Players.Select(x => (x.Id, x.Position)));
            Assert.Equal(a.Leaving.Select(x => (x.Id, x.Position)), b.Leaving.Select(x => (x.Id, x.Position)));
            Assert.Equal(a.Puck, b.Puck);
        }
    }

    private static double Reference(ulong seed, int id, int k)
    {
        unchecked
        {
            const ulong g = 0x9E3779B97F4A7C15UL;
            ulong z = seed + (g * (ulong)(id + 1)) + ((ulong)k * g);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (z >> 11) * Math.Pow(2, -53);
        }
    }

    private bool SamePeriod(int a, int b) => P.Timeline.Segments[a].Period == P.Timeline.Segments[b].Period;

    private bool ApproachBetween(double from, double to) =>
        P.Segments.Any(s => s.ApproachSeconds > 0 && s.Start <= to && s.GapStart >= from);
}
