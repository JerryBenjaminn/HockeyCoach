using HockeyCoach.Replay.Core;
using HockeyCoach.Replay.Tests.Support;

namespace HockeyCoach.Replay.Tests;

/// <summary>The presentation time warp on the seed 42 acceptance match with the real <c>data/replay.json</c> (D-088).</summary>
[Collection(Seed42Collection.Name)]
public sealed class Seed42PresentationTests
{
    private const double ShiftGameSeconds = 45;
    private readonly Seed42Match _m;

    public Seed42PresentationTests(Seed42Match m)
    {
        _m = m;
    }

    private PresentationTimeline P => _m.Presentation;

    private IEnumerable<PresentationPhase> AllPhases => P.Segments.SelectMany(s => s.Phases);

    [Fact]
    public void Segments_TileThePresentationAxis_OnePerEvent()
    {
        Assert.Equal(_m.Timeline.Segments.Count, P.Segments.Count);
        Assert.Equal(0, P.Segments[0].Start);
        for (int i = 0; i < P.Segments.Count; i++)
        {
            SegmentTiming s = P.Segments[i];
            Assert.Equal(i, s.Index);
            Assert.True(s.Start <= s.GapStart && s.GapStart <= s.WindowStart && s.WindowStart <= s.EventEnd && s.EventEnd <= s.End);
            if (i > 0)
            {
                Assert.Equal(P.Segments[i - 1].End, s.Start);
            }

            double t = s.Start;
            foreach (PresentationPhase ph in s.Phases)
            {
                Assert.Equal(t, ph.Start);
                Assert.True(ph.Duration > 0);
                t = ph.End;
            }

            Assert.Equal(s.End, t);
        }

        Assert.Equal(P.Segments[^1].End, P.Duration);
    }

    [Fact]
    public void Clock_IsMonotone_OverAllPhases()
    {
        double clock = 0;
        foreach (PresentationPhase ph in AllPhases)
        {
            Assert.True(ph.ClockStart >= clock, "phase of segment " + ph.SegmentIndex + " starts before the previous clock");
            Assert.True(ph.ClockEnd >= ph.ClockStart);
            clock = ph.ClockEnd;
        }

        Assert.Equal(_m.Timeline.EndClock, clock);
    }

    [Fact]
    public void EveryEventEnd_MapsToItsExactClockTime()
    {
        var cursor = new ReplayCursor(P);
        for (int i = 0; i < P.Segments.Count; i++)
        {
            TimelineSegment seg = _m.Timeline.Segments[i];
            Assert.Equal(seg.EndClock, P.ClockAt(i, P.Segments[i].EventEnd));
            cursor.SeekEvent(i);
            ReplaySample s = cursor.Sample();
            Assert.Equal(seg.EndClock, s.Clock);
            Assert.Equal(seg.Period, s.Period);
            Assert.Equal(seg.Time, s.PeriodTime, 9);
        }
    }

    [Fact]
    public void FrozenPhases_FreezeTheClock()
    {
        var frozenKinds = new[]
        {
            PresentationPhaseKind.FaceoffApproach, PresentationPhaseKind.GoalHold, PresentationPhaseKind.StoppageHold,
            PresentationPhaseKind.PeriodBreak, PresentationPhaseKind.TimeoutHold,
        };
        var cursor = new ReplayCursor(P);
        int frozen = 0;
        foreach (PresentationPhase ph in AllPhases.Where(ph => frozenKinds.Contains(ph.Kind)))
        {
            Assert.True(ph.IsFrozen);
            foreach (double f in new[] { 0.0, 0.25, 0.5, 0.99 })
            {
                cursor.SeekTime(ph.Start + (f * ph.Duration));
                Assert.Equal(ph.ClockStart, cursor.Clock);
            }

            frozen++;
        }

        int stoppages = _m.Match.Events.Count(e => e.Type == "Stoppage");
        Assert.Equal(stoppages, AllPhases.Count(ph => ph.Kind == PresentationPhaseKind.StoppageHold));
        Assert.Equal(_m.Match.Events.Count(e => e.Type == "Stoppage" && e.Reason == "PeriodEnd"), AllPhases.Count(ph => ph.Kind == PresentationPhaseKind.PeriodBreak));
        Assert.True(frozen > stoppages);
    }

    [Fact]
    public void FaceoffsAfterAStoppage_HaveAClampedApproach_AndPeriodStartFaceoffsNone()
    {
        IReadOnlyList<TimelineSegment> segs = _m.Timeline.Segments;
        int approaches = 0;
        for (int i = 0; i < P.Segments.Count; i++)
        {
            if (segs[i].Type != ReplayEventType.Faceoff)
            {
                Assert.Equal(0, P.Segments[i].ApproachSeconds);
                continue;
            }

            int j = i - 1;
            while (j >= 0 && segs[j].Period == segs[i].Period && segs[j].Type == ReplayEventType.LineChange)
            {
                j--;
            }

            bool afterStoppage = j >= 0 && segs[j].Period == segs[i].Period && segs[j].Type == ReplayEventType.Stoppage;
            if (afterStoppage)
            {
                Assert.InRange(P.Segments[i].ApproachSeconds, _m.Config.Timing.FaceoffApproachMinSeconds, _m.Config.Timing.FaceoffApproachMaxSeconds);
                approaches++;
            }
            else
            {
                Assert.Equal(0, P.Segments[i].ApproachSeconds);
            }
        }

        Assert.True(approaches > 20);
    }

    [Fact]
    public void LineChangesAtAStoppage_AreInstant()
    {
        foreach (SegmentTiming s in P.Segments.Where(s => (s.Flags & SegmentTimingFlags.StoppageSeries) != 0))
        {
            Assert.Equal(ReplayEventType.LineChange, _m.Timeline.Segments[s.Index].Type);
            Assert.Equal(0, s.Duration);
        }

        Assert.True(P.Segments.Count(s => (s.Flags & SegmentTimingFlags.StoppageSeries) != 0) > 20);
    }

    [Fact]
    public void Goals_AreSlowedAndHeld()
    {
        List<SegmentTiming> goals = P.Segments.Where(s => (s.Flags & SegmentTimingFlags.Goal) != 0).ToList();
        Assert.Equal(_m.Timeline.Goals.Count, goals.Count);
        Assert.Equal(_m.Timeline.Goals.Select(g => g.SegmentIndex), goals.Select(g => g.Index));
        foreach (SegmentTiming s in goals)
        {
            double unslowed = Math.Max(s.Split.Action / _m.Config.Timing.ActionCompression, s.PuckFloorSeconds);
            Assert.True(s.Durations.Window >= PresentationTiming.GoalWindow(unslowed, _m.Config.Goal) - 1e-9);
            Assert.True(s.Durations.Window >= _m.Config.Goal.SlowMoMinSeconds);
            Assert.Equal(PresentationPhaseKind.GoalHold, s.Phases[^1].Kind);
            Assert.Equal(_m.Config.Goal.HoldSeconds, s.Phases[^1].Duration, 9);
        }
    }

    [Fact]
    public void EveryGap_RespectsItsFloors()
    {
        foreach (SegmentTiming s in P.Segments)
        {
            ReplayEventType type = _m.Timeline.Segments[s.Index].Type;
            if ((s.Flags & SegmentTimingFlags.InstantLineChange) != 0 || type == ReplayEventType.Timeout)
            {
                continue;
            }

            double min = (s.Flags & SegmentTimingFlags.FlyingLineChange) != 0 ? _m.Config.Timing.LineChangeSeconds : _m.Config.Timing.MinEventSeconds;
            Assert.True(s.Durations.Total >= min - 1e-9);
            Assert.True(s.Durations.Total >= s.SkaterFloorSeconds - 1e-9);
            if ((s.Flags & SegmentTimingFlags.Goal) == 0)
            {
                Assert.True(s.Durations.Window >= s.PuckFloorSeconds - 1e-9);
            }
        }
    }

    [Fact]
    public void DeadTime_IsAboutFortyPercentOfTheMatchClock()
    {
        // data-schema.md, replay.json, Esitysaika 3: "Siemenellä 42 kuollutta aikaa on n. 40 % ottelukellosta".
        double dead = P.Segments.Sum(s => s.Split.Dead);
        Assert.InRange(dead / _m.Timeline.EndClock, 0.35, 0.45);
    }

    [Fact]
    public void A45SecondShift_LastsWithinTheTarget_At1x()
    {
        double shift = P.Duration * ShiftGameSeconds / _m.Timeline.EndClock;
        Assert.InRange(shift, PresentationTimelineTests.ShiftTargetMin, PresentationTimelineTests.ShiftTargetMax);
    }

    [Fact]
    public void SeekingAPresentationTime_GivesTheSameSampleAsPlayingToIt()
    {
        var played = new ReplayCursor(P) { Speed = 8 };
        played.Play();
        int frames = 0;
        while (played.IsPlaying)
        {
            played.Advance(1.0 / 30);
            if (frames++ % 7 == 0)
            {
                var seeker = new ReplayCursor(P);
                seeker.SeekTime(played.Time);
                AssertSame(played.Sample(), seeker.Sample());
            }
        }

        Assert.True(frames > 1000);
    }

    [Fact]
    public void Build_IsDeterministic()
    {
        PresentationTimeline again = PresentationTimeline.Build(TimelineBuilder.Build(ReplayJsonReader.Read(_m.Json)), ReplayConfigFile.Read());
        Assert.Equal(P.Duration, again.Duration);
        for (int i = 0; i < P.Segments.Count; i++)
        {
            SegmentTiming a = P.Segments[i];
            SegmentTiming b = again.Segments[i];
            Assert.Equal((a.Start, a.GapStart, a.WindowStart, a.EventEnd, a.End, a.Flags), (b.Start, b.GapStart, b.WindowStart, b.EventEnd, b.End, b.Flags));
        }
    }

    [Fact]
    public void SeekClock_ShowsTheStateAfterEveryEventUpToThatClock()
    {
        IReadOnlyList<TimelineSegment> segs = _m.Timeline.Segments;
        var cursor = new ReplayCursor(P);
        for (int i = 0; i < segs.Count - 1; i++)
        {
            cursor.SeekClock(segs[i].EndClock);
            int last = i;
            while (last + 1 < segs.Count && segs[last + 1].EndClock <= segs[i].EndClock)
            {
                last++;
            }

            Assert.Equal(last, cursor.SegmentIndex);
            Assert.Equal(segs[i].EndClock, cursor.Clock);
            Assert.Equal(P.Segments[last].EventEnd, cursor.Time);
        }
    }

    [Fact]
    public void TargetTime_AnticipatesOrdinaryGaps_ButNotStoppagesFaceoffsOrPeriodStarts()
    {
        Seed42Match m = _m;
        SkaterSettings skaters = m.Config.Skaters;
        foreach (SegmentTiming s in m.Presentation.Segments)
        {
            ReplayEventType type = m.Timeline.Segments[s.Index].Type;
            double target = SkaterArrivals.TargetTime(s, skaters);
            bool anticipates = type != ReplayEventType.Stoppage && type != ReplayEventType.Faceoff
                && (s.Flags & (SegmentTimingFlags.PeriodStart | SegmentTimingFlags.StoppageSeries | SegmentTimingFlags.InstantLineChange)) == 0;
            Assert.Equal(anticipates, s.UsesAnticipation);
            Assert.Equal(anticipates ? s.EventEnd - skaters.AnticipationSeconds : s.EventEnd, target);
        }
    }

    private static void AssertSame(ReplaySample a, ReplaySample b)
    {
        Assert.Equal(a.SegmentIndex, b.SegmentIndex);
        Assert.Equal(a.Time, b.Time);
        Assert.Equal(a.Fraction, b.Fraction);
        Assert.Equal(a.Clock, b.Clock);
        Assert.Equal(a.PeriodTime, b.PeriodTime);
        Assert.Equal((a.HomeScore, a.AwayScore), (b.HomeScore, b.AwayScore));
        Assert.Equal(a.Puck, b.Puck);
        Assert.Equal(a.PuckCarrierId, b.PuckCarrierId);
        Assert.Equal(a.Players.Select(p => (p.Id, p.Position)), b.Players.Select(p => (p.Id, p.Position)));
    }
}
