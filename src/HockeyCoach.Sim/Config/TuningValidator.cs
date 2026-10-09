using System;
using System.Collections.Generic;
using System.Globalization;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Validates tuning objects after loading (docs/data-schema.md, tuning.json → Validointi). Returns every
    /// problem found, each prefixed with its data path, so the Harness can report them all at once.
    /// An empty list means valid.
    /// </summary>
    public static class TuningValidator
    {
        /// <summary>
        /// Tolerance when checking that weights sum to 1 (docs/data-schema.md: 1e-6). A numeric tolerance, not a balance value.
        /// </summary>
        public const double WeightSumTolerance = 1e-6;

        /// <summary>Validates the whole tuning config against the rink's xG zone names.</summary>
        public static IReadOnlyList<string> Validate(TuningConfig tuning, IReadOnlyList<string> xgZones)
        {
            var errors = new List<string>();
            if (tuning == null)
            {
                errors.Add("tuning: missing");
                return errors;
            }

            errors.AddRange(Validate(tuning.Stats));
            errors.AddRange(Validate(tuning.CheckFormula, tuning.Stats));
            errors.AddRange(Validate(tuning.Checks.Values));
            errors.AddRange(Validate(tuning.Shot, xgZones));
            if (!IsFinite(tuning.Positions.OffSideCheckModifier))
            {
                errors.Add("positions.offSideCheckModifier: must be a finite number");
            }

            errors.AddRange(Validate(tuning.Time));
            errors.AddRange(Validate(tuning.Plays));
            errors.AddRange(Validate(tuning.ChanceTypes));
            errors.AddRange(Validate(tuning.ChanceClasses));
            return errors;
        }

        /// <summary>
        /// Validates the time section: at least one period, positive period and shift lengths, non-negative setup,
        /// regroup and action times, and every <see cref="TimeConfig.PlayActionKeys"/> present.
        /// </summary>
        public static IReadOnlyList<string> Validate(TimeConfig time)
        {
            var errors = new List<string>();
            if (time == null)
            {
                errors.Add("time: missing");
                return errors;
            }

            if (time.Periods < 1)
            {
                errors.Add("time.periods: must be at least 1, was " + time.Periods);
            }

            RequirePositive("time.periodSeconds", time.PeriodSeconds, errors);
            RequirePositive("time.forwardShiftSeconds", time.ForwardShiftSeconds, errors);
            RequirePositive("time.defenceShiftSeconds", time.DefenceShiftSeconds, errors);
            RequireNonNegative("time.setupSeconds", time.SetupSeconds, errors);
            RequireNonNegative("time.regroupSeconds", time.RegroupSeconds, errors);
            foreach (KeyValuePair<string, double> action in time.SecondsPerAction)
            {
                RequireNonNegative("time.secondsPerAction." + action.Key, action.Value, errors);
            }

            foreach (string key in TimeConfig.PlayActionKeys)
            {
                if (!time.SecondsPerAction.ContainsKey(key))
                {
                    errors.Add("time.secondsPerAction." + key + ": missing (used by the play actions)");
                }
            }

            return errors;
        }

        /// <summary>Validates the plays section: maxBeats and maxNodesPerBeat are at least 1.</summary>
        public static IReadOnlyList<string> Validate(PlaysConfig plays)
        {
            var errors = new List<string>();
            if (plays == null)
            {
                errors.Add("plays: missing");
                return errors;
            }

            if (plays.MaxBeats < 1)
            {
                errors.Add("plays.maxBeats: must be at least 1, was " + plays.MaxBeats);
            }

            if (plays.MaxNodesPerBeat < 1)
            {
                errors.Add("plays.maxNodesPerBeat: must be at least 1, was " + plays.MaxNodesPerBeat);
            }

            return errors;
        }

        /// <summary>Validates the chanceTypes section: organization threshold in [0, 1], non-negative window.</summary>
        public static IReadOnlyList<string> Validate(ChanceTypesConfig chanceTypes)
        {
            var errors = new List<string>();
            if (chanceTypes == null)
            {
                errors.Add("chanceTypes: missing");
                return errors;
            }

            RequireUnit("chanceTypes.rushOrganizationBelow", chanceTypes.RushOrganizationBelow, errors);
            RequireNonNegative("chanceTypes.turnoverWindowSeconds", chanceTypes.TurnoverWindowSeconds, errors);
            return errors;
        }

        /// <summary>Validates the chanceClasses section: thresholds in [0, 1] and top ≥ good ≥ moderate.</summary>
        public static IReadOnlyList<string> Validate(ChanceClassesConfig chanceClasses)
        {
            var errors = new List<string>();
            if (chanceClasses == null)
            {
                errors.Add("chanceClasses: missing");
                return errors;
            }

            RequireUnit("chanceClasses.topMinXg", chanceClasses.TopMinXg, errors);
            RequireUnit("chanceClasses.goodMinXg", chanceClasses.GoodMinXg, errors);
            RequireUnit("chanceClasses.moderateMinXg", chanceClasses.ModerateMinXg, errors);
            if (!(chanceClasses.TopMinXg >= chanceClasses.GoodMinXg && chanceClasses.GoodMinXg >= chanceClasses.ModerateMinXg))
            {
                errors.Add("chanceClasses: thresholds must satisfy topMinXg >= goodMinXg >= moderateMinXg");
            }

            return errors;
        }

        /// <summary>Validates the stat scale: min &lt; max and grades cover min..max without gaps or overlaps.</summary>
        public static IReadOnlyList<string> Validate(StatsConfig stats)
        {
            var errors = new List<string>();
            if (stats == null)
            {
                errors.Add("stats: missing");
                return errors;
            }

            if (stats.Min >= stats.Max)
            {
                errors.Add("stats: min (" + stats.Min + ") must be less than max (" + stats.Max + ")");
            }

            if (stats.Grades.Count == 0)
            {
                errors.Add("stats.grades: must not be empty");
                return errors;
            }

            var grades = new List<StatGrade>(stats.Grades);
            grades.Sort((a, b) => a.Min != b.Min ? a.Min.CompareTo(b.Min) : string.CompareOrdinal(a.Name, b.Name));
            int expectedMin = stats.Min;
            foreach (StatGrade grade in grades)
            {
                string path = "stats.grades." + grade.Name;
                if (grade.Min > grade.Max)
                {
                    errors.Add(path + ": min (" + grade.Min + ") is greater than max (" + grade.Max + ")");
                }

                if (grade.Min != expectedMin)
                {
                    errors.Add(path + ": starts at " + grade.Min + " but the previous range ends at " + (expectedMin - 1) + " (gap or overlap)");
                }

                expectedMin = grade.Max + 1;
            }

            if (expectedMin - 1 != stats.Max)
            {
                errors.Add("stats.grades: ranges end at " + (expectedMin - 1) + " but stats.max is " + stats.Max);
            }

            return errors;
        }

        /// <summary>
        /// Validates the global check formula parameters. When <paramref name="stats"/> is given, also checks
        /// that referenceValue lies within stats.min..stats.max.
        /// </summary>
        public static IReadOnlyList<string> Validate(CheckFormulaConfig formula, StatsConfig stats = null)
        {
            var errors = new List<string>();
            if (formula == null)
            {
                errors.Add("checkFormula: missing");
                return errors;
            }

            if (!IsFinite(formula.K) || formula.K < 0.0)
            {
                errors.Add("checkFormula.k: must be a finite number >= 0, was " + Format(formula.K));
            }

            ValidateBounds("checkFormula", formula.MinProbability, formula.MaxProbability, errors);

            if (!IsFinite(formula.ReferenceValue))
            {
                errors.Add("checkFormula.referenceValue: must be a finite number, was " + Format(formula.ReferenceValue));
            }
            else if (stats != null && (formula.ReferenceValue < stats.Min || formula.ReferenceValue > stats.Max))
            {
                errors.Add("checkFormula.referenceValue: must be within stats.min..stats.max (" + stats.Min + ".." + stats.Max + "), was " + Format(formula.ReferenceValue));
            }

            return errors;
        }

        /// <summary>
        /// Validates one check definition according to its kind (D-017, D-019): present sides sum to 1,
        /// absent sides are empty, p0 and shares lie in (0, 1), role and stat names match.
        /// </summary>
        public static IReadOnlyList<string> Validate(CheckDefinition check)
        {
            var errors = new List<string>();
            if (check == null)
            {
                errors.Add("checks: null check definition");
                return errors;
            }

            string path = "checks." + check.Id;
            if (string.IsNullOrEmpty(check.Id))
            {
                errors.Add("checks: check id must not be empty");
            }

            switch (check.Kind)
            {
                case CheckKind.TwoSided:
                    if (check.Side.HasValue)
                    {
                        errors.Add(path + ".side: only oneSided checks have a side");
                    }

                    ValidateP0(path, check.P0, errors);
                    ValidateSide(path + ".attacker", check.Attacker, errors);
                    ValidateSide(path + ".defender", check.Defender, errors);
                    break;

                case CheckKind.OneSided:
                    ValidateP0(path, check.P0, errors);
                    if (!check.Side.HasValue)
                    {
                        errors.Add(path + ".side: oneSided check needs side attacker or defender");
                        break;
                    }

                    bool attackerPresent = check.Side.Value == CheckSide.Attacker;
                    ValidateSide(path + (attackerPresent ? ".attacker" : ".defender"), attackerPresent ? check.Attacker : check.Defender, errors);
                    if ((attackerPresent ? check.Defender : check.Attacker).Count > 0)
                    {
                        errors.Add(path + (attackerPresent ? ".defender" : ".attacker") + ": must be absent in a oneSided check with side " + (attackerPresent ? "attacker" : "defender"));
                    }

                    break;

                case CheckKind.NoCheck:
                    if (check.Side.HasValue)
                    {
                        errors.Add(path + ".side: only oneSided checks have a side");
                    }

                    if (check.P0.HasValue)
                    {
                        errors.Add(path + ".p0: noCheck must not have p0");
                    }

                    if (check.Attacker.Count > 0)
                    {
                        errors.Add(path + ".attacker: noCheck must not have an attacker side");
                    }

                    if (check.Defender.Count > 0)
                    {
                        errors.Add(path + ".defender: noCheck must not have a defender side");
                    }

                    break;

                default:
                    errors.Add(path + ".kind: unknown kind " + check.Kind);
                    break;
            }

            ValidateModifiers(path + ".modifiers", check.Modifiers, errors);
            ValidateParameters(path, check.Parameters, errors);
            return errors;
        }

        /// <summary>Validates several check definitions, in the order given, and rejects duplicate ids.</summary>
        public static IReadOnlyList<string> Validate(IEnumerable<CheckDefinition> checks)
        {
            var errors = new List<string>();
            if (checks == null)
            {
                errors.Add("checks: missing");
                return errors;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (CheckDefinition check in checks)
            {
                if (check != null && !ids.Add(check.Id))
                {
                    errors.Add("checks." + check.Id + ": duplicate check id");
                }

                errors.AddRange(Validate(check));
            }

            return errors;
        }

        /// <summary>
        /// Validates the shot check: goalie side and every zone's shooter weights sum to 1, baseXg and
        /// attackerByXgZone cover exactly <paramref name="xgZones"/>, base rates in (0, 1) and not above the shot
        /// maxProbability (D-029), onTargetShare in (0, 1),
        /// and the shot's own probability bounds (D-014).
        /// </summary>
        public static IReadOnlyList<string> Validate(ShotConfig shot, IReadOnlyList<string> xgZones)
        {
            const string path = "checks.shot";
            var errors = new List<string>();
            if (shot == null)
            {
                errors.Add(path + ": missing");
                return errors;
            }

            ValidateSide(path + ".defender", shot.Defender, errors);
            foreach (KeyValuePair<string, WeightTerm[]> zone in shot.AttackerByXgZone)
            {
                ValidateSide(path + ".attackerByXgZone." + zone.Key, zone.Value, errors);
            }

            foreach (KeyValuePair<string, double> zone in shot.BaseXg)
            {
                if (!IsOpenUnit(zone.Value))
                {
                    errors.Add(path + ".baseXg." + zone.Key + ": must be in (0, 1), was " + Format(zone.Value));
                }
                else if (zone.Value > shot.MaxProbability)
                {
                    errors.Add(path + ".baseXg." + zone.Key + ": must not exceed checks.shot.maxProbability (" + Format(shot.MaxProbability) + "), was " + Format(zone.Value) + " (D-029)");
                }
            }

            if (xgZones != null)
            {
                ValidateZoneCoverage(path + ".baseXg", shot.BaseXg.Keys, xgZones, errors);
                ValidateZoneCoverage(path + ".attackerByXgZone", shot.AttackerByXgZone.Keys, xgZones, errors);
            }

            if (!IsOpenUnit(shot.OnTargetShare))
            {
                errors.Add(path + ".onTargetShare: must be in (0, 1), was " + Format(shot.OnTargetShare));
            }

            ValidateBounds(path, shot.MinProbability, shot.MaxProbability, errors);
            ValidateModifiers(path + ".modifiers", shot.Modifiers, errors);
            ValidateParameters(path, shot.Parameters, errors);
            return errors;
        }

        private static void ValidateP0(string path, double? p0, List<string> errors)
        {
            if (!p0.HasValue)
            {
                errors.Add(path + ".p0: missing");
            }
            else if (!IsOpenUnit(p0.Value))
            {
                errors.Add(path + ".p0: must be in (0, 1), was " + Format(p0.Value));
            }
        }

        private static void ValidateBounds(string path, double min, double max, List<string> errors)
        {
            if (!IsOpenUnit(min))
            {
                errors.Add(path + ".minProbability: must be in (0, 1), was " + Format(min));
            }

            if (!IsOpenUnit(max))
            {
                errors.Add(path + ".maxProbability: must be in (0, 1), was " + Format(max));
            }

            if (!(min < max))
            {
                errors.Add(path + ": minProbability (" + Format(min) + ") must be less than maxProbability (" + Format(max) + ")");
            }
        }

        private static void ValidateSide(string path, IReadOnlyList<WeightTerm> terms, List<string> errors)
        {
            if (terms.Count == 0)
            {
                errors.Add(path + ": side is missing or empty");
                return;
            }

            double sum = 0.0;
            for (int i = 0; i < terms.Count; i++)
            {
                WeightTerm term = terms[i];
                string termPath = path + "." + term.Participant + "." + StatName(term.Stat);
                if (!CheckRoles.IsKnown(term.Participant))
                {
                    errors.Add(path + "." + term.Participant + ": unknown participant role");
                }
                else if (CheckRoles.StatOwnerOf(term.Participant) != term.Stat.Owner)
                {
                    errors.Add(termPath + ": role " + term.Participant + " reads " + (term.Stat.Owner == StatOwner.Goalie ? "skater" : "goalie") + " stats");
                }

                if (!IsFinite(term.Weight) || term.Weight < 0.0)
                {
                    errors.Add(termPath + ": weight must be a finite number >= 0, was " + Format(term.Weight));
                }

                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(terms[j].Participant, term.Participant, StringComparison.Ordinal) && terms[j].Stat.Equals(term.Stat))
                    {
                        errors.Add(termPath + ": listed twice");
                    }
                }

                sum += term.Weight;
            }

            if (Math.Abs(sum - 1.0) > WeightSumTolerance)
            {
                errors.Add(path + ": weights must sum to 1, sum was " + Format(sum));
            }
        }

        private static void ValidateModifiers(string path, CheckModifiers modifiers, List<string> errors)
        {
            foreach (KeyValuePair<string, double> modifier in modifiers.Scalars)
            {
                if (!IsFinite(modifier.Value))
                {
                    errors.Add(path + "." + modifier.Key + ": must be a finite number");
                }
            }

            foreach (string table in modifiers.TableNames)
            {
                double[] values = modifiers.GetTable(table);
                if (values.Length == 0)
                {
                    errors.Add(path + "." + table + ": table must not be empty");
                }

                for (int i = 0; i < values.Length; i++)
                {
                    if (!IsFinite(values[i]))
                    {
                        errors.Add(path + "." + table + "[" + i + "]: must be a finite number");
                    }
                }
            }
        }

        private static void ValidateParameters(string path, IReadOnlyDictionary<string, double> parameters, List<string> errors)
        {
            foreach (KeyValuePair<string, double> parameter in parameters)
            {
                if (!IsFinite(parameter.Value))
                {
                    errors.Add(path + "." + parameter.Key + ": must be a finite number");
                }
                else if (parameter.Key.EndsWith("Share", StringComparison.Ordinal) && !IsOpenUnit(parameter.Value))
                {
                    errors.Add(path + "." + parameter.Key + ": share must be in (0, 1), was " + Format(parameter.Value));
                }
            }
        }

        private static void ValidateZoneCoverage(string path, IEnumerable<string> actual, IReadOnlyList<string> expected, List<string> errors)
        {
            var actualSet = new SortedSet<string>(actual, StringComparer.Ordinal);
            var expectedSet = new SortedSet<string>(expected, StringComparer.Ordinal);
            foreach (string zone in expectedSet)
            {
                if (!actualSet.Contains(zone))
                {
                    errors.Add(path + ": missing xG zone " + zone + " (rink.json xgZones)");
                }
            }

            foreach (string zone in actualSet)
            {
                if (!expectedSet.Contains(zone))
                {
                    errors.Add(path + "." + zone + ": not an xG zone in rink.json");
                }
            }
        }

        private static void RequirePositive(string path, double value, List<string> errors)
        {
            if (!IsFinite(value) || value <= 0.0)
            {
                errors.Add(path + ": must be a finite number > 0, was " + Format(value));
            }
        }

        private static void RequireNonNegative(string path, double value, List<string> errors)
        {
            if (!IsFinite(value) || value < 0.0)
            {
                errors.Add(path + ": must be a finite number >= 0, was " + Format(value));
            }
        }

        private static void RequireUnit(string path, double value, List<string> errors)
        {
            if (!IsFinite(value) || value < 0.0 || value > 1.0)
            {
                errors.Add(path + ": must be in [0, 1], was " + Format(value));
            }
        }

        private static string StatName(StatRef stat)
        {
            return stat.Owner == StatOwner.Skater ? StatNames.ToName(stat.SkaterStat) : StatNames.ToName(stat.GoalieStat);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsOpenUnit(double value)
        {
            return value > 0.0 && value < 1.0;
        }

        private static string Format(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
