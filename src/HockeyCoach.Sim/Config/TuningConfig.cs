using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Typed balance values from tuning.json for the sections used so far (stats, checkFormula, checks, positions,
    /// time, plays, chanceTypes, chanceClasses). Sections for later milestones are added as they are implemented.
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
        public TuningConfig(
            StatsConfig stats,
            CheckFormulaConfig checkFormula,
            IEnumerable<CheckDefinition> checks,
            ShotConfig shot,
            PositionsConfig positions,
            TimeConfig time,
            PlaysConfig plays,
            ChanceTypesConfig chanceTypes,
            ChanceClassesConfig chanceClasses)
        {
            Time = time ?? throw new ArgumentNullException(nameof(time));
            Plays = plays ?? throw new ArgumentNullException(nameof(plays));
            ChanceTypes = chanceTypes ?? throw new ArgumentNullException(nameof(chanceTypes));
            ChanceClasses = chanceClasses ?? throw new ArgumentNullException(nameof(chanceClasses));
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
