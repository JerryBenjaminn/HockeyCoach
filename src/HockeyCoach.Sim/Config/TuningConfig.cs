using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Typed balance values from tuning.json (stats, checkFormula, checks, positions, time, energy, organization, pressure,
    /// familiarity, plays, transitions, chanceTypes, chanceClasses, loosePuckSpots). form and chemistry are not used yet.
    /// </summary>
    public sealed class TuningConfig
    {
        private readonly SortedDictionary<string, CheckDefinition> _checks;

        /// <summary>Creates the tuning config. Use <see cref="TuningValidator"/> to check it.</summary>
        /// <param name="stats">Stat scale and grades.</param>
        /// <param name="checkFormula">Check formula parameters.</param>
        /// <param name="checks">All checks except <c>shot</c>.</param>
        /// <param name="shot">The shot goal check.</param>
        /// <param name="positions">Position balance values.</param>
        /// <param name="time">Time values.</param>
        /// <param name="plays">Play limits.</param>
        /// <param name="chanceTypes">Chance type classification.</param>
        /// <param name="chanceClasses">Chance class xG thresholds.</param>
        /// <param name="pressure">Physical pressure radius.</param>
        /// <param name="loosePuckSpots">Loose-puck spot rules.</param>
        /// <param name="energy">Energy values (O-5).</param>
        /// <param name="organization">Organization values (O-6).</param>
        /// <param name="familiarity">Familiarity values (O-11).</param>
        /// <param name="transitions">Rush values (O-7).</param>
        public TuningConfig(
            StatsConfig stats,
            CheckFormulaConfig checkFormula,
            IEnumerable<CheckDefinition> checks,
            ShotConfig shot,
            PositionsConfig positions,
            TimeConfig time,
            PlaysConfig plays,
            ChanceTypesConfig chanceTypes,
            ChanceClassesConfig chanceClasses,
            PressureConfig pressure,
            LoosePuckSpotsConfig loosePuckSpots,
            EnergyConfig energy,
            OrganizationConfig organization,
            FamiliarityConfig familiarity,
            TransitionsConfig transitions)
        {
            Energy = energy ?? throw new ArgumentNullException(nameof(energy));
            Organization = organization ?? throw new ArgumentNullException(nameof(organization));
            Familiarity = familiarity ?? throw new ArgumentNullException(nameof(familiarity));
            Transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
            Time = time ?? throw new ArgumentNullException(nameof(time));
            Plays = plays ?? throw new ArgumentNullException(nameof(plays));
            ChanceTypes = chanceTypes ?? throw new ArgumentNullException(nameof(chanceTypes));
            ChanceClasses = chanceClasses ?? throw new ArgumentNullException(nameof(chanceClasses));
            Pressure = pressure ?? throw new ArgumentNullException(nameof(pressure));
            LoosePuckSpots = loosePuckSpots ?? throw new ArgumentNullException(nameof(loosePuckSpots));
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            CheckFormula = checkFormula ?? throw new ArgumentNullException(nameof(checkFormula));
            Shot = shot ?? throw new ArgumentNullException(nameof(shot));
            Positions = positions ?? throw new ArgumentNullException(nameof(positions));
            if (checks == null)
            {
                throw new ArgumentNullException(nameof(checks));
            }

            _checks = new SortedDictionary<string, CheckDefinition>(StringComparer.Ordinal);
            foreach (CheckDefinition check in checks)
            {
                if (check == null)
                {
                    throw new ArgumentException("Null check.", nameof(checks));
                }

                if (_checks.ContainsKey(check.Id))
                {
                    throw new ArgumentException("Duplicate check id " + check.Id + ".", nameof(checks));
                }

                _checks.Add(check.Id, check);
            }
        }

        /// <summary>Stat scale and grades.</summary>
        public StatsConfig Stats { get; }

        /// <summary>Check formula parameters.</summary>
        public CheckFormulaConfig CheckFormula { get; }

        /// <summary>All checks except the shot, in ordinal id order.</summary>
        public IReadOnlyDictionary<string, CheckDefinition> Checks
        {
            get { return _checks; }
        }

        /// <summary>The shot goal check.</summary>
        public ShotConfig Shot { get; }

        /// <summary>Position balance values.</summary>
        public PositionsConfig Positions { get; }

        /// <summary>Time values (game seconds).</summary>
        public TimeConfig Time { get; }

        /// <summary>Play limits.</summary>
        public PlaysConfig Plays { get; }

        /// <summary>Chance type classification.</summary>
        public ChanceTypesConfig ChanceTypes { get; }

        /// <summary>Chance class xG thresholds.</summary>
        public ChanceClassesConfig ChanceClasses { get; }

        /// <summary>Team pressure state values (O-9) and the physical pressure radius (M-7).</summary>
        public PressureConfig Pressure { get; }

        /// <summary>Loose-puck spot rules (D-044).</summary>
        public LoosePuckSpotsConfig LoosePuckSpots { get; }

        /// <summary>Energy values (O-5).</summary>
        public EnergyConfig Energy { get; }

        /// <summary>Organization values (O-6).</summary>
        public OrganizationConfig Organization { get; }

        /// <summary>Familiarity values (O-11).</summary>
        public FamiliarityConfig Familiarity { get; }

        /// <summary>Rush values (O-7).</summary>
        public TransitionsConfig Transitions { get; }

        /// <summary>Returns a check by id; throws if it does not exist.</summary>
        public CheckDefinition GetCheck(string id)
        {
            if (id != null && _checks.TryGetValue(id, out CheckDefinition check))
            {
                return check;
            }

            throw new KeyNotFoundException("Unknown check " + id + ".");
        }
    }
}
