using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Events;

namespace HockeyCoach.AI
{
    /// <summary>The observed transition style of one opponent possession (data-schema.md AI-2, A-T2).</summary>
    public enum TransitionStyleClass
    {
        /// <summary>The first action of the possession had no play id: a rush (O-7).</summary>
        Rush = 0,

        /// <summary>The first action of the possession had a play id: a regroup into a play (D-036).</summary>
        Regroup = 1,
    }

    /// <summary>One classified opponent possession (A-T2 item 2).</summary>
    public sealed class TransitionObservation
    {
        /// <summary>Creates the observation.</summary>
        /// <param name="eventIndex">Index of the classified event in the log.</param>
        /// <param name="style">Rush or regroup.</param>
        /// <param name="systemId">The event's system id: the observing (defending) team's own system at that moment.</param>
        public TransitionObservation(int eventIndex, TransitionStyleClass style, string systemId)
        {
            EventIndex = eventIndex;
            Style = style;
            SystemId = systemId ?? throw new ArgumentNullException(nameof(systemId));
        }

        /// <summary>Index of the classified event in the log.</summary>
        public int EventIndex { get; }

        /// <summary>Rush or regroup.</summary>
        public TransitionStyleClass Style { get; }

        /// <summary>The observing team's own system when the opponent made the action (D-045).</summary>
        public string SystemId { get; }
    }

    /// <summary>
    /// Classifies the opponent's transitions from the public event log (data-schema.md AI-2, A-T2 item 2): the first
    /// action event of an opponent possession that began in transition (not from a faceoff) is a rush when its play id
    /// is empty and a regroup when it is set. Pure and deterministic; uses only public log fields (D-077).
    /// </summary>
    public static class TransitionStyle
    {
        private const int Nobody = 0;
        private const int Own = 1;
        private const int Opponent = 2;

        /// <summary>
        /// Every classified opponent possession in log order. Observations whose event has no system id are not returned.
        /// </summary>
        /// <param name="events">The public event log from the start of the match.</param>
        /// <param name="ownSkaters">The observing team's skater ids; any other actor belongs to the opponent.</param>
        public static IReadOnlyList<TransitionObservation> Classify(IReadOnlyList<SimEvent> events, SortedSet<int> ownSkaters)
        {
            if (events == null || ownSkaters == null)
            {
                throw new ArgumentNullException(events == null ? nameof(events) : nameof(ownSkaters));
            }

            var observations = new List<TransitionObservation>();
            int holder = Nobody;
            bool fromTransition = false;
            bool ownPending = false;
            bool opponentPending = false;
            int period = 0;

            for (int i = 0; i < events.Count; i++)
            {
                SimEvent e = events[i];
                if (e.Context.Period != period)
                {
                    // Period start: same as a stoppage (item 2.1).
                    period = e.Context.Period;
                    holder = Nobody;
                    fromTransition = false;
                    ownPending = false;
                    opponentPending = false;
                }

                if (e is StoppageEvent)
                {
                    holder = Nobody;
                    fromTransition = false;
                    ownPending = false;
                    opponentPending = false;
                    continue;
                }

                if (e is FaceoffEvent faceoff)
                {
                    int centre = faceoff.Winner == TeamSide.Home ? faceoff.HomeCentreId : faceoff.AwayCentreId;
                    holder = TeamOf(centre, ownSkaters);
                    fromTransition = false;
                    ownPending = false;
                    opponentPending = false;
                    continue;
                }

                int? actor = PublicLog.Actor(e);
                if (!actor.HasValue)
                {
                    // Puck battle, line change, timeout: neither end nor start a possession.
                    continue;
                }

                int team = TeamOf(actor.Value, ownSkaters);
                if (team != holder)
                {
                    // Item 2.3: the puck changed teams without a public takeaway.
                    holder = team;
                    fromTransition = true;
                    if (team == Own)
                    {
                        ownPending = true;
                    }
                    else
                    {
                        opponentPending = true;
                    }
                }

                bool pending = team == Own ? ownPending : opponentPending;
                if (pending && fromTransition)
                {
                    // Item 2.4: the first action of a transition possession.
                    if (team == Own)
                    {
                        ownPending = false;
                    }
                    else
                    {
                        opponentPending = false;
                        if (!(e is TurnoverEvent) && !string.IsNullOrEmpty(e.Context.SystemId))
                        {
                            TransitionStyleClass style = string.IsNullOrEmpty(e.Context.PlayId) ? TransitionStyleClass.Rush : TransitionStyleClass.Regroup;
                            observations.Add(new TransitionObservation(i, style, e.Context.SystemId));
                        }
                    }
                }

                if (e is TurnoverEvent turnover)
                {
                    // Item 2.5: the taker's team starts a transition possession.
                    holder = TeamOf(turnover.TakerId, ownSkaters);
                    fromTransition = true;
                    if (holder == Own)
                    {
                        ownPending = true;
                    }
                    else
                    {
                        opponentPending = true;
                    }
                }
            }

            return observations;
        }

        private static int TeamOf(int playerId, SortedSet<int> ownSkaters)
        {
            return ownSkaters.Contains(playerId) ? Own : Opponent;
        }
    }
}
