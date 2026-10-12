using System;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// Presentation durations of the three phases of one gap (data-schema.md, replay.json, Esitysaika): dead time,
    /// other action and the event's own action window, in presentation seconds at 1×.
    /// </summary>
    public readonly struct GapDurations
    {
        /// <summary>Creates the durations.</summary>
        public GapDurations(double dead, double other, double window)
        {
            Dead = dead;
            Other = other;
            Window = window;
        }

        /// <summary>Dead time (setup, regroup), k / <c>deadCompression</c>.</summary>
        public double Dead { get; }

        /// <summary>Other action, (r − k) / <c>actionCompression</c>.</summary>
        public double Other { get; }

        /// <summary>The event's action window, a / <c>actionCompression</c>.</summary>
        public double Window { get; }

        /// <summary>Total duration of the gap.</summary>
        public double Total => Dead + Other + Window;

        /// <summary>
        /// Stretches all phases in the same ratio so that the window lasts at least <paramref name="floor"/>. When the
        /// window is empty it cannot be stretched by a ratio; then the window alone gets the floor.
        /// </summary>
        public GapDurations WithWindowAtLeast(double floor)
        {
            if (!(Window < floor))
            {
                return this;
            }

            if (Window > 0)
            {
                double s = floor / Window;
                return new GapDurations(Dead * s, Other * s, floor);
            }

            return new GapDurations(Dead, Other, floor);
        }

        /// <summary>
        /// Stretches all phases in the same ratio so that the gap lasts at least <paramref name="floor"/>. An empty gap
        /// cannot be stretched by a ratio; then the window gets the floor (the event's own action is what is shown).
        /// </summary>
        public GapDurations WithTotalAtLeast(double floor)
        {
            double total = Total;
            if (!(total < floor))
            {
                return this;
            }

            if (total > 0)
            {
                double s = floor / total;
                return new GapDurations(Dead * s, Other * s, Window * s);
            }

            return new GapDurations(0, 0, floor);
        }

        /// <summary>Replaces the window duration.</summary>
        public GapDurations WithWindow(double window)
        {
            return new GapDurations(Dead, Other, window);
        }
    }

    /// <summary>
    /// Pure functions of the presentation time warp (D-088 A, data-schema.md, replay.json, Esitysaika). Times named
    /// "game" are match clock seconds, the rest presentation seconds at 1×.
    /// </summary>
    public static class PresentationTiming
    {
        /// <summary>
        /// The action cost a of an event in game seconds, before the cap at the gap: <c>faceoff</c>, <c>pass</c>,
        /// <c>shoot</c>, <c>dumpIn</c>; <c>PuckBattle</c> = <c>loosePuck</c> (+ <c>rebound</c> after a shot,
        /// + <c>systemStep</c> after a puck battle); <c>ControlledZoneEntry</c> = <c>skate</c> when carried, 0 when passed;
        /// <c>Turnover</c> = <c>skate</c>; <c>Stoppage</c>, <c>LineChange</c> and <c>Timeout</c> 0.
        /// </summary>
        /// <param name="e">The event.</param>
        /// <param name="previous">The previous event of the log, null for the first.</param>
        /// <param name="costs">The header's time costs.</param>
        public static double ActionCost(ReplayEvent e, ReplayEvent previous, TimeCostsData costs)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            if (costs == null)
            {
                throw new ArgumentNullException(nameof(costs));
            }

            switch (e.Kind)
            {
                case ReplayEventType.Faceoff:
                    return costs.Action("faceoff");
                case ReplayEventType.Pass:
                    return costs.Action("pass");
                case ReplayEventType.Shot:
                    return costs.Action("shoot");
                case ReplayEventType.DumpIn:
                    return costs.Action("dumpIn");
                case ReplayEventType.PuckBattle:
                    double battle = costs.Action("loosePuck");
                    if (previous != null && previous.Kind == ReplayEventType.Shot)
                    {
                        battle += costs.Action("rebound");
                    }
                    else if (previous != null && previous.Kind == ReplayEventType.PuckBattle)
                    {
                        battle += costs.Action("systemStep");
                    }

                    return battle;
                case ReplayEventType.ControlledZoneEntry:
                    return e.Method == "Carry" ? costs.Action("skate") : 0;
                case ReplayEventType.Turnover:
                    return costs.Action("skate");
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Dead time k of a gap in game seconds from the remainder r = g − a: setup + regroup when r reaches both,
        /// setup when r reaches setup, otherwise 0.
        /// </summary>
        public static double DeadSeconds(double remainder, double setupSeconds, double regroupSeconds)
        {
            if (remainder >= setupSeconds + regroupSeconds)
            {
                return setupSeconds + regroupSeconds;
            }

            return remainder >= setupSeconds ? setupSeconds : 0;
        }

        /// <summary>
        /// Presentation durations of a gap of <paramref name="gap"/> game seconds whose own action costs
        /// <paramref name="action"/> (capped at the gap), before any floor.
        /// </summary>
        public static GapDurations Split(double gap, double action, TimeCostsData costs, TimingSettings timing)
        {
            if (costs == null)
            {
                throw new ArgumentNullException(nameof(costs));
            }

            if (timing == null)
            {
                throw new ArgumentNullException(nameof(timing));
            }

            GameSplit s = SplitGame(gap, action, costs);
            return new GapDurations(s.Dead / timing.DeadCompression, s.Other / timing.ActionCompression, s.Action / timing.ActionCompression);
        }

        /// <summary>The gap split in game seconds: action a (capped at the gap), dead time k and other action r − k.</summary>
        public static GameSplit SplitGame(double gap, double action, TimeCostsData costs)
        {
            if (costs == null)
            {
                throw new ArgumentNullException(nameof(costs));
            }

            double g = Math.Max(0, gap);
            double a = Math.Max(0, Math.Min(action, g));
            double r = g - a;
            double k = DeadSeconds(r, costs.SetupSeconds, costs.RegroupSeconds);
            return new GameSplit(k, r - k, a);
        }

        /// <summary>
        /// Duration of the frozen approach before a faceoff: the largest displacement over
        /// <c>faceoffApproachSpeed</c>, clamped to [<c>faceoffApproachMinSeconds</c>, <c>faceoffApproachMaxSeconds</c>].
        /// </summary>
        public static double ApproachSeconds(double maxDisplacementMetres, TimingSettings timing)
        {
            if (timing == null)
            {
                throw new ArgumentNullException(nameof(timing));
            }

            double seconds = Math.Max(0, maxDisplacementMetres) / timing.FaceoffApproachSpeed;
            return Math.Max(timing.FaceoffApproachMinSeconds, Math.Min(timing.FaceoffApproachMaxSeconds, seconds));
        }

        /// <summary>
        /// Duration of the frozen change phase of a stoppage series (D-089 A1): the outgoing players' largest distance to
        /// their bench exit over <c>stoppageChangeSpeed</c>, clamped to [<c>stoppageChangeMinSeconds</c>,
        /// <c>stoppageChangeMaxSeconds</c>].
        /// </summary>
        public static double ChangeSeconds(double maxDisplacementMetres, TimingSettings timing)
        {
            if (timing == null)
            {
                throw new ArgumentNullException(nameof(timing));
            }

            double seconds = Math.Max(0, maxDisplacementMetres) / timing.StoppageChangeSpeed;
            return Math.Max(timing.StoppageChangeMinSeconds, Math.Min(timing.StoppageChangeMaxSeconds, seconds));
        }

        /// <summary>A goal's action window in slow motion: window / <c>slowMoRate</c>, at least <c>slowMoMinSeconds</c>.</summary>
        public static double GoalWindow(double window, GoalSettings goal)
        {
            if (goal == null)
            {
                throw new ArgumentNullException(nameof(goal));
            }

            return Math.Max(window / goal.SlowMoRate, goal.SlowMoMinSeconds);
        }
    }

    /// <summary>A gap split in game seconds (data-schema.md, replay.json, Esitysaika, steps 1–3).</summary>
    public readonly struct GameSplit
    {
        /// <summary>Creates the split.</summary>
        public GameSplit(double dead, double other, double action)
        {
            Dead = dead;
            Other = other;
            Action = action;
        }

        /// <summary>Dead time k.</summary>
        public double Dead { get; }

        /// <summary>Other action r − k.</summary>
        public double Other { get; }

        /// <summary>The event's own action a.</summary>
        public double Action { get; }
    }
}
