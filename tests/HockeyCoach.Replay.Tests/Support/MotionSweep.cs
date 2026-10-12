using HockeyCoach.Replay.Core;

namespace HockeyCoach.Replay.Tests.Support;

/// <summary>
/// The whole seed 42 match sampled at 60 Hz of presentation time (1×), computed once per presentation timeline and
/// shared by the motion tests.
/// </summary>
public sealed class MotionSweep
{
    /// <summary>Frame length in presentation seconds (60 Hz at 1×).</summary>
    public const double Frame = 1.0 / 60;

    private static readonly Dictionary<PresentationTimeline, MotionSweep> Cache = new();

    private MotionSweep(PresentationTimeline p)
    {
        int frames = (int)Math.Floor(p.Duration / Frame);
        var list = new List<SweepFrame>(frames + 1);
        for (int f = 0; f <= frames; f++)
        {
            double t = f * Frame;
            int index = p.SegmentIndexAt(t);
            list.Add(new SweepFrame(t, index, p.Motion.Skaters.Sample(index, t), p.Motion.Sample(index, t)));
        }

        Frames = list;
    }

    /// <summary>The frames in time order.</summary>
    public IReadOnlyList<SweepFrame> Frames { get; }

    /// <summary>The sweep of <paramref name="p"/>.</summary>
    public static MotionSweep Of(PresentationTimeline p)
    {
        lock (Cache)
        {
            if (!Cache.TryGetValue(p, out MotionSweep? sweep))
            {
                sweep = new MotionSweep(p);
                Cache[p] = sweep;
            }

            return sweep;
        }
    }
}

/// <summary>One frame of a <see cref="MotionSweep"/>.</summary>
public sealed record SweepFrame(double Time, int Segment, SkaterPose[] Skaters, MotionSample Sample);
