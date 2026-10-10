using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Config
{
    /// <summary>tuning.json <c>organization</c> (data-schema.md O-6, D-007, D-062). Organization is 0..1 per team.</summary>
    public sealed class OrganizationConfig
    {
        private readonly SortedDictionary<SkaterStat, double> _recoveryWeights;

        /// <summary>Creates the config. Use <see cref="TuningValidator"/> to check it.</summary>
        /// <param name="dropDefensive">Drop when the puck is lost in the losing team's defensive zone.</param>
        /// <param name="dropNeutral">Drop in the neutral zone.</param>
        /// <param name="dropOffensive">Drop in the offensive zone.</param>
        /// <param name="dropFactorOnShotOrDump">Multiplier of the drop after a shot or dump.</param>
        /// <param name="dropPerCommittedPlayer">Extra drop per committed player (O-10).</param>
        /// <param name="recoveryPerSecond">Recovery per game second.</param>
        /// <param name="recoveryWeights">Stat weights of the recovery stat.</param>
        /// <param name="recoveryPerStatPoint">Recovery factor slope per stat point.</param>
        /// <param name="organizedThreshold">Rush ends and chance type stops being rush at or above this.</param>
        public OrganizationConfig(
            double dropDefensive,
            double dropNeutral,
            double dropOffensive,
            double dropFactorOnShotOrDump,
            double dropPerCommittedPlayer,
            double recoveryPerSecond,
            IEnumerable<KeyValuePair<SkaterStat, double>> recoveryWeights,
            double recoveryPerStatPoint,
            double organizedThreshold)
        {
            if (recoveryWeights == null)
            {
                throw new ArgumentNullException(nameof(recoveryWeights));
            }

            DropDefensive = dropDefensive;
            DropNeutral = dropNeutral;
            DropOffensive = dropOffensive;
            DropFactorOnShotOrDump = dropFactorOnShotOrDump;
            DropPerCommittedPlayer = dropPerCommittedPlayer;
            RecoveryPerSecond = recoveryPerSecond;
            RecoveryPerStatPoint = recoveryPerStatPoint;
            OrganizedThreshold = organizedThreshold;
            _recoveryWeights = new SortedDictionary<SkaterStat, double>();
            foreach (KeyValuePair<SkaterStat, double> weight in recoveryWeights)
            {
                _recoveryWeights[weight.Key] = weight.Value;
            }
        }

        /// <summary>Drop in the losing team's defensive zone.</summary>
        public double DropDefensive { get; }

        /// <summary>Drop in the neutral zone.</summary>
        public double DropNeutral { get; }

        /// <summary>Drop in the offensive zone.</summary>
        public double DropOffensive { get; }

        /// <summary>Multiplier of the drop after a shot or dump.</summary>
        public double DropFactorOnShotOrDump { get; }

        /// <summary>Extra drop per committed player.</summary>
        public double DropPerCommittedPlayer { get; }

        /// <summary>Recovery per game second.</summary>
        public double RecoveryPerSecond { get; }

        /// <summary>Stat weights of the recovery stat, in stat order.</summary>
        public IReadOnlyDictionary<SkaterStat, double> RecoveryWeights
        {
            get { return _recoveryWeights; }
        }

        /// <summary>Recovery factor slope per stat point.</summary>
        public double RecoveryPerStatPoint { get; }

        /// <summary>Organized threshold.</summary>
        public double OrganizedThreshold { get; }

        /// <summary>The drop for a zone (losing team's view).</summary>
        public double DropIn(RinkZone zone)
        {
            switch (zone)
            {
                case RinkZone.Defensive: return DropDefensive;
                case RinkZone.Neutral: return DropNeutral;
                default: return DropOffensive;
            }
        }
    }
}
