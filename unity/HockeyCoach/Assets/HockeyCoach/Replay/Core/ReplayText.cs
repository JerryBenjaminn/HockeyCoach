using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// Display texts of the replay view (HUD and disc labels), built from the timeline only. Plain English, invariant
    /// culture; nothing here affects playback.
    /// </summary>
    public static class ReplayText
    {
        /// <summary>Seconds as <c>mm:ss</c> (whole seconds, rounded down; negative values show as 00:00).</summary>
        public static string Clock(double seconds)
        {
            long whole = seconds > 0 ? (long)Math.Floor(seconds) : 0;
            return (whole / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (whole % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        /// <summary>Period and clock, e.g. <c>P2 07:41</c>.</summary>
        public static string PeriodClock(int period, double periodTime)
        {
            return "P" + period.ToString(CultureInfo.InvariantCulture) + " " + Clock(periodTime);
        }

        /// <summary>Score line, e.g. <c>Home 1 - 0 Away</c>.</summary>
        public static string Score(int home, int away)
        {
            return "Home " + home.ToString(CultureInfo.InvariantCulture) + " - " + away.ToString(CultureInfo.InvariantCulture) + " Away";
        }

        /// <summary>Speed, e.g. <c>8x</c>.</summary>
        public static string Speed(double speed)
        {
            return speed.ToString("0.##", CultureInfo.InvariantCulture) + "x";
        }

        /// <summary>Disc label: the roster position on the first line and the id on the second (just the id when the player is not in the roster).</summary>
        public static string PlayerLabel(RosterEntry entry, int id)
        {
            string number = id.ToString(CultureInfo.InvariantCulture);
            if (entry == null || string.IsNullOrEmpty(entry.Position))
            {
                return number;
            }

            return entry.Position + "\n" + number;
        }

        /// <summary>A player reference for event texts, e.g. <c>#13 LD</c>.</summary>
        public static string Player(ReplayTimeline timeline, int? id)
        {
            if (!id.HasValue)
            {
                return "?";
            }

            string text = "#" + id.Value.ToString(CultureInfo.InvariantCulture);
            RosterEntry entry = timeline != null ? timeline.FindPlayer(id.Value) : null;
            return entry != null && !string.IsNullOrEmpty(entry.Position) ? text + " " + entry.Position : text;
        }

        /// <summary>Event heading, e.g. <c>Event 12/874 Pass</c> (1-based).</summary>
        public static string EventHeading(TimelineSegment segment, int eventCount)
        {
            return "Event " + (segment.Index + 1).ToString(CultureInfo.InvariantCulture) + "/" + eventCount.ToString(CultureInfo.InvariantCulture) + " " + segment.Event.Type;
        }

        /// <summary>One-line description of the segment's event with the actors' ids and positions.</summary>
        public static string Describe(TimelineSegment segment, ReplayTimeline timeline)
        {
            if (segment == null)
            {
                throw new ArgumentNullException(nameof(segment));
            }

            ReplayEvent e = segment.Event;
            switch (e.Kind)
            {
                case ReplayEventType.Faceoff:
                    return "Faceoff " + Player(timeline, e.HomeCentreId) + " vs " + Player(timeline, e.AwayCentreId) + ", won by " + Or(e.Winner) + Paren(e.Location);
                case ReplayEventType.ControlledZoneEntry:
                    return "Zone entry by " + Player(timeline, e.CarrierId) + Paren(Join(e.Method, e.Numbers)) + ": " + Or(e.Outcome);
                case ReplayEventType.DumpIn:
                    return "Dump-in by " + Player(timeline, e.ShooterId) + ", battle: " + Or(e.BattleOutcome);
                case ReplayEventType.Pass:
                    return "Pass " + Player(timeline, e.PasserId) + " -> " + Player(timeline, e.ReceiverId) + ", "
                        + (e.Succeeded == true ? "completed" : "failed") + (e.UnderPressure == true ? ", under pressure" : string.Empty);
                case ReplayEventType.Shot:
                    return DescribeShot(segment, timeline);
                case ReplayEventType.Turnover:
                    return "Turnover " + Player(timeline, e.LoserId) + " -> " + Player(timeline, e.TakerId);
                case ReplayEventType.PuckBattle:
                    return "Puck battle " + Players(timeline, e.AttackerIds) + " vs " + Players(timeline, e.DefenderIds) + ": " + Or(e.Outcome);
                case ReplayEventType.Timeout:
                    return "Timeout: " + Or(e.Team);
                case ReplayEventType.Stoppage:
                    return "Stoppage: " + Or(e.Reason);
                case ReplayEventType.LineChange:
                    return "Line change: out " + Players(timeline, e.OutgoingIds) + "; in " + Players(timeline, e.IncomingIds);
                default:
                    return e.Type ?? string.Empty;
            }
        }

        private static string DescribeShot(TimelineSegment segment, ReplayTimeline timeline)
        {
            ReplayEvent e = segment.Event;
            var text = new StringBuilder("Shot by ").Append(Player(timeline, e.ShooterId)).Append(": ");
            if (segment.ScoringTeam.HasValue)
            {
                text.Append("GOAL (").Append(segment.ScoringTeam.Value == ReplayTeam.Home ? "Home" : "Away").Append(')');
            }
            else
            {
                text.Append(Or(e.Outcome));
                if (e.StoppedById.HasValue)
                {
                    text.Append(" by ").Append(Player(timeline, e.StoppedById));
                }
            }

            if (e.Xg.HasValue)
            {
                text.Append(", xG ").Append(e.Xg.Value.ToString("0.00", CultureInfo.InvariantCulture));
            }

            return text.ToString();
        }

        private static string Players(ReplayTimeline timeline, List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return "-";
            }

            var parts = new string[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                parts[i] = Player(timeline, ids[i]);
            }

            return string.Join(", ", parts);
        }

        private static string Or(string value)
        {
            return string.IsNullOrEmpty(value) ? "?" : value;
        }

        private static string Paren(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : " (" + value + ")";
        }

        private static string Join(string a, string b)
        {
            if (string.IsNullOrEmpty(a))
            {
                return b;
            }

            return string.IsNullOrEmpty(b) ? a : a + ", " + b;
        }
    }
}
