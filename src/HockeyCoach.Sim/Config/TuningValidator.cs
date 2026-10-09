using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Validates tuning objects after loading. Returns every problem found, each prefixed with a data path,
    /// so that the Harness can report them all at once. An empty list means valid.
    /// </summary>
    public static class TuningValidator
    {
        /// <summary>
        /// Floating point tolerance when checking that weights sum to 1. A numeric tolerance, not a balance value.
        /// </summary>
        public const double WeightSumTolerance = 1e-9;

        /// <summary>Validates the global check formula parameters.</summary>
        public static IReadOnlyList<string> Validate(CheckFormulaConfig formula)
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

            if (!IsOpenUnit(formula.MinProbability))
            {
                errors.Add("checkFormula.minProbability: must be in (0, 1), was " + Format(formula.MinProbability));
            }

            if (!IsOpenUnit(formula.MaxProbability))
            {
                errors.Add("checkFormula.maxProbability: must be in (0, 1), was " + Format(formula.MaxProbability));
            }

            if (!(formula.MinProbability < formula.MaxProbability))
            {
                errors.Add("checkFormula: minProbability (" + Format(formula.MinProbability) + ") must be less than maxProbability (" + Format(formula.MaxProbability) + ")");
            }

            return errors;
        }

        /// <summary>
        /// Validates one check definition: p0 in (0, 1); on each side weights are finite and non-negative,
        /// participants are named, no (participant, stat) pair repeats, and the weights sum to 1 (D-017).
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

            if (!IsOpenUnit(check.P0))
            {
                errors.Add(path + ".p0: must be in (0, 1), was " + Format(check.P0));
            }

            ValidateSide(path + ".attacker", check.Attacker, errors);
            ValidateSide(path + ".defender", check.Defender, errors);
            return errors;
        }

        /// <summary>Validates several check definitions, in the order given.</summary>
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

        private static void ValidateSide(string path, IReadOnlyList<WeightTerm> terms, List<string> errors)
        {
            double sum = 0.0;
            for (int i = 0; i < terms.Count; i++)
            {
                WeightTerm term = terms[i];
                string termPath = path + "." + term.Participant + "." + term.Stat;
                if (string.IsNullOrEmpty(term.Participant))
                {
                    errors.Add(path + "[" + i + "]: participant name must not be empty");
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
            return value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
