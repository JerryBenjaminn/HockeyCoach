using HockeyCoach.Replay.Core;

namespace HockeyCoach.Replay.Tests.Support;

/// <summary>
/// Orbiting metric (D-089 A2): for every pair of drawn skaters, over each run of consecutive frames in which they are
/// within <c>distance</c> of each other, the direction between them must not turn more than 90° between any two frames
/// at most <c>window</c> presentation seconds apart.
/// </summary>
public static class OrbitMetric
{
    public sealed record Result(int Violations, long ContactFrames, double MaxRotationDegrees, string Worst);

    public static Result Measure(MotionSweep sweep, double distance, double window, Dictionary<string, int>? categories = null, bool settledOnly = false)
    {
        int frames = Math.Max(1, (int)Math.Round(window / MotionSweep.Frame));
        var history = new Dictionary<(int, int), List<(double Time, double Angle)>>();
        int violations = 0;
        long contact = 0;
        double max = 0;
        string worst = string.Empty;
        foreach (SweepFrame f in sweep.Frames)
        {
            var seen = new HashSet<(int, int)>();
            SkaterPose[] s = f.Skaters;
            for (int a = 0; a < s.Length; a++)
            {
                for (int b = a + 1; b < s.Length; b++)
                {
                    if (s[a].IsWaiting || s[b].IsWaiting || (settledOnly && (s[a].IsChanging || s[b].IsChanging)))
                    {
                        continue;
                    }

                    double dx = s[b].Position.X - s[a].Position.X;
                    double dy = s[b].Position.Y - s[a].Position.Y;
                    if (Math.Sqrt((dx * dx) + (dy * dy)) > distance)
                    {
                        continue;
                    }

                    var key = (s[a].Id, s[b].Id);
                    seen.Add(key);
                    contact++;
                    double angle = Math.Atan2(dy, dx);
                    if (!history.TryGetValue(key, out var run))
                    {
                        run = new List<(double, double)>();
                        history[key] = run;
                    }

                    run.Add((f.Time, angle));
                    while (run.Count > frames + 1)
                    {
                        run.RemoveAt(0);
                    }

                    foreach ((double t, double old) in run)
                    {
                        double turn = Math.Abs(Math.IEEERemainder(angle - old, 2 * Math.PI)) * 180 / Math.PI;
                        if (turn > max)
                        {
                            max = turn;
                            worst = "players " + key + " at " + f.Time.ToString("F2") + " turned " + turn.ToString("F0") + "°";
                        }

                        if (turn > 90)
                        {
                            violations++;
                            if (categories != null)
                            {
                                string cat = (s[a].Team == s[b].Team ? "team" : "opp") + (s[a].IsChanging || s[b].IsChanging ? "+changing" : "");
                                categories[cat] = categories.TryGetValue(cat, out int n) ? n + 1 : 1;
                            }

                            break;
                        }
                    }
                }
            }

            foreach (var key in history.Keys.Where(k => !seen.Contains(k)).ToList())
            {
                history.Remove(key);
            }
        }

        return new Result(violations, contact, max, worst);
    }
}
