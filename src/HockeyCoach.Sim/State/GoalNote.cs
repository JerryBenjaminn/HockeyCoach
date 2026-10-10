using HockeyCoach.Sim.Events;

namespace HockeyCoach.Sim.State
{
    /// <summary>
    /// Why a goal happened, for the one-sentence explanation of the match output (approved milestone 3 plan). Not an
    /// event: the event schema stays as it is; this is report data the log does not carry (defence organization).
    /// </summary>
    public sealed class GoalNote
    {
        /// <summary>Creates the note.</summary>
        public GoalNote(
            int period,
            double time,
            TeamSide team,
            int shooterId,
            ChanceType chanceType,
            double defenceOrganization,
            bool hasEntry,
            EntryNumbers entry,
            bool royalRoad,
            bool screen,
            bool crease,
            bool secondChance)
        {
            Period = period;
            Time = time;
            Team = team;
            ShooterId = shooterId;
            ChanceType = chanceType;
            DefenceOrganization = defenceOrganization;
            HasEntry = hasEntry;
            Entry = entry;
            RoyalRoad = royalRoad;
            Screen = screen;
            Crease = crease;
            SecondChance = secondChance;
        }

        /// <summary>Period number.</summary>
        public int Period { get; }

        /// <summary>Period time of the goal.</summary>
        public double Time { get; }

        /// <summary>Scoring team.</summary>
        public TeamSide Team { get; }

        /// <summary>Scorer id.</summary>
        public int ShooterId { get; }

        /// <summary>Chance type (O-12).</summary>
        public ChanceType ChanceType { get; }

        /// <summary>The defending team's organization at the shot (0..1).</summary>
        public double DefenceOrganization { get; }

        /// <summary>Whether the possession had a controlled zone entry.</summary>
        public bool HasEntry { get; }

        /// <summary>The entry's N vs M (M-8), if any.</summary>
        public EntryNumbers Entry { get; }

        /// <summary>Royal Road (M-3).</summary>
        public bool RoyalRoad { get; }

        /// <summary>Screen (M-4).</summary>
        public bool Screen { get; }

        /// <summary>Crease xG (D-047, D-059).</summary>
        public bool Crease { get; }

        /// <summary>Second-chance shot (D-059).</summary>
        public bool SecondChance { get; }
    }
}
