using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.AI
{
    /// <summary>
    /// Reads the public event log for the rule-based and adaptive tactics (data-schema.md AI-2). An event shows which
    /// system the opponent defended with when its actor (passer, shooter, zone-entry carrier, dump-in shooter, or the
    /// player who lost the puck in a turnover) is one of the coach's own skaters and the event carries a system id
    /// (D-045: the defending team's system).
    /// </summary>
    public static class PublicLog
    {
        /// <summary>Match second of an event: (period − 1) × period length + clock (AI-2).</summary>
        public static double MatchSecond(int period, double time, double periodSeconds)
        {
            return ((period - 1) * periodSeconds) + time;
        }

        /// <summary>Own skater ids of the view's team.</summary>
        public static SortedSet<int> OwnSkaters(CoachView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            var ids = new SortedSet<int>();
            foreach (Skater s in view.Team.Skaters)
            {
                ids.Add(s.Id);
            }

            return ids;
        }

        /// <summary>The acting player of an event, or null for events without one actor.</summary>
        public static int? Actor(SimEvent e)
        {
            switch (e)
            {
                case PassEvent pass:
                    return pass.PasserId;
                case ShotEvent shot:
                    return shot.ShooterId;
                case ControlledZoneEntryEvent entry:
                    return entry.CarrierId;
                case DumpInEvent dump:
                    return dump.ShooterId;
                case TurnoverEvent turnover:
                    return turnover.LoserId;
                default:
                    return null;
            }
        }

        /// <summary>The system the opponent defended with in <paramref name="e"/>, or null if it does not show one.</summary>
        public static string OpponentSystem(SimEvent e, SortedSet<int> ownSkaters)
        {
            int? actor = Actor(e);
            return actor.HasValue && ownSkaters.Contains(actor.Value) && e.Context.SystemId != null ? e.Context.SystemId : null;
        }

        /// <summary>The most recent system the opponent defended with (S-T step 2), or null.</summary>
        public static string LastOpponentSystem(CoachView view)
        {
            SortedSet<int> own = OwnSkaters(view);
            for (int i = view.Events.Count - 1; i >= 0; i--)
            {
                string system = OpponentSystem(view.Events[i], own);
                if (system != null)
                {
                    return system;
                }
            }

            return null;
        }
    }
}
