using System;
using System.Collections.Generic;
using System.Globalization;

namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Validates tuning.json <c>ai</c> (data-schema.md AI-3): value ranges against <c>time</c>, and the system and play ids
    /// against the loaded data (cross-check in the same <c>validate</c> run). Errors are prefixed with their data path.
    /// </summary>
    public static class AiConfigValidator
    {
        /// <summary>Largest absolute play bonus (AI-3: [−2, 2]).</summary>
        public const double MaxPlayBonus = 2.0;

        /// <summary>Validates the values of <paramref name="ai"/>.</summary>
        public static IReadOnlyList<string> Validate(AiConfig ai, TimeConfig time)
        {
            var errors = new List<string>();
            if (ai == null)
            {
                errors.Add("ai: missing");
                return errors;
            }

            double periodSeconds = time == null ? double.PositiveInfinity : time.PeriodSeconds;
            RequireUnit("ai.random.onTheFlyChangeProbability", ai.Random.OnTheFlyChangeProbability, errors);
            RequireUnit("ai.random.timeoutProbabilityPerStoppage", ai.Random.TimeoutProbabilityPerStoppage, errors);
            RequireUnit("ai.estimate.extraDrainPerSecondOnIce", ai.Estimate.ExtraDrainPerSecondOnIce, errors);

            AiRulebasedConfig rule = ai.Rulebased;
            RequireUnit("ai.rulebased.changeEnergyThreshold", rule.ChangeEnergyThreshold, errors);
            RequireUnit("ai.rulebased.freshnessMargin", rule.FreshnessMargin, errors);
            RequirePeriodWindow("ai.rulebased.lateGameSeconds", rule.LateGameSeconds, periodSeconds, errors);
            RequireAtLeastOne("ai.rulebased.protectLeadGoals", rule.ProtectLeadGoals, errors);
            RequireAtLeastOne("ai.rulebased.chaseDeficitGoals", rule.ChaseDeficitGoals, errors);
            foreach (string key in AiRulebasedConfig.ModeKeys)
            {
                if (!rule.Modes.ContainsKey(key))
                {
                    errors.Add("ai.rulebased.modes." + key + ": missing");
                }
            }

            foreach (KeyValuePair<string, AiModeConfig> mode in rule.Modes)
            {
                string path = "ai.rulebased.modes." + mode.Key;
                if (!Contains(AiRulebasedConfig.ModeKeys, mode.Key))
                {
                    errors.Add(path + ": unknown mode (expected normal, protect or chase)");
                    continue;
                }

                if (!Contains(AiModeConfig.TransitionNames, mode.Value.Transition))
                {
                    errors.Add(path + ".transition: must be rush or regroup, got " + mode.Value.Transition);
                }

                if (mode.Value.LooseChasers != 1 && mode.Value.LooseChasers != 2)
                {
                    errors.Add(path + ".looseChasers: must be 1 or 2, got " + mode.Value.LooseChasers.ToString(CultureInfo.InvariantCulture));
                }
            }

            foreach (KeyValuePair<string, IReadOnlyDictionary<string, double>> system in rule.PlayBonusBySystem)
            {
                foreach (KeyValuePair<string, double> bonus in system.Value)
                {
                    if (!(bonus.Value >= -MaxPlayBonus && bonus.Value <= MaxPlayBonus))
                    {
                        errors.Add("ai.rulebased.playBonusBySystem." + system.Key + "." + bonus.Key + ": must be in [-2, 2], got " + Format(bonus.Value));
                    }
                }
            }

            foreach (KeyValuePair<string, string> entry in rule.TransitionBySystem)
            {
                if (!Contains(AiModeConfig.TransitionNames, entry.Value))
                {
                    errors.Add("ai.rulebased.transitionBySystem." + entry.Key + ": must be rush or regroup, got " + entry.Value);
                }
            }

            AiTimeoutConfig timeout = rule.Timeout;
            RequireUnit("ai.rulebased.timeout.tiredEnergy", timeout.TiredEnergy, errors);
            RequirePeriodWindow("ai.rulebased.timeout.lateGameSeconds", timeout.LateGameSeconds, periodSeconds, errors);
            RequireAtLeastOne("ai.rulebased.timeout.minDeficitGoals", timeout.MinDeficitGoals, errors);
            if (timeout.MaxDeficitGoals < timeout.MinDeficitGoals)
            {
                errors.Add("ai.rulebased.timeout.maxDeficitGoals: must be at least minDeficitGoals ("
                    + timeout.MinDeficitGoals.ToString(CultureInfo.InvariantCulture) + "), got " + timeout.MaxDeficitGoals.ToString(CultureInfo.InvariantCulture));
            }

            AiAdaptiveConfig adaptive = ai.Adaptive;
            if (!(adaptive.WindowSeconds > 0.0) || double.IsInfinity(adaptive.WindowSeconds))
            {
                errors.Add("ai.adaptive.windowSeconds: must be a finite number > 0, got " + Format(adaptive.WindowSeconds));
            }

            if (!(adaptive.PriorWeight >= 0.0) || double.IsInfinity(adaptive.PriorWeight))
            {
                errors.Add("ai.adaptive.priorWeight: must be a finite number >= 0, got " + Format(adaptive.PriorWeight));
            }

            if (string.Equals(adaptive.PassiveSystem, adaptive.AggressiveSystem, StringComparison.Ordinal))
            {
                errors.Add("ai.adaptive.aggressiveSystem: must differ from passiveSystem (" + adaptive.PassiveSystem + ")");
            }

            RequireAtLeastOne("ai.adaptive.styleWindowEvents", adaptive.StyleWindowEvents, errors);
            RequireAtLeastOne("ai.adaptive.styleMinEvents", adaptive.StyleMinEvents, errors);
            if (adaptive.StyleMinEvents > adaptive.StyleWindowEvents)
            {
                errors.Add("ai.adaptive.styleMinEvents: must be at most styleWindowEvents ("
                    + adaptive.StyleWindowEvents.ToString(CultureInfo.InvariantCulture) + "), got " + adaptive.StyleMinEvents.ToString(CultureInfo.InvariantCulture));
            }

            RequireUnit("ai.adaptive.rushShareForPassive", adaptive.RushShareForPassive, errors);
            RequireUnit("ai.adaptive.rushShareForAggressive", adaptive.RushShareForAggressive, errors);
            if (adaptive.RushShareForAggressive >= adaptive.RushShareForPassive)
            {
                errors.Add("ai.adaptive.rushShareForAggressive: must be below rushShareForPassive ("
                    + Format(adaptive.RushShareForPassive) + "), got " + Format(adaptive.RushShareForAggressive));
            }

            return errors;
        }

        /// <summary>Checks every system and play id in <paramref name="ai"/> against the loaded systems and plays.</summary>
        public static IReadOnlyList<string> ValidateReferences(AiConfig ai, IEnumerable<string> systemIds, IEnumerable<string> playIds)
        {
            var errors = new List<string>();
            if (ai == null || systemIds == null || playIds == null)
            {
                throw new ArgumentNullException(ai == null ? nameof(ai) : systemIds == null ? nameof(systemIds) : nameof(playIds));
            }

            var systems = new SortedSet<string>(systemIds, StringComparer.Ordinal);
            var plays = new SortedSet<string>(playIds, StringComparer.Ordinal);
            foreach (KeyValuePair<string, AiModeConfig> mode in ai.Rulebased.Modes)
            {
                RequireKnown("ai.rulebased.modes." + mode.Key + ".system", mode.Value.System, systems, "system", errors);
            }

            foreach (KeyValuePair<string, IReadOnlyDictionary<string, double>> system in ai.Rulebased.PlayBonusBySystem)
            {
                string path = "ai.rulebased.playBonusBySystem." + system.Key;
                if (!systems.Contains(system.Key))
                {
                    errors.Add(path + ": unknown system " + system.Key);
                }

                foreach (KeyValuePair<string, double> bonus in system.Value)
                {
                    if (!plays.Contains(bonus.Key))
                    {
                        errors.Add(path + "." + bonus.Key + ": unknown play " + bonus.Key);
                    }
                }
            }

            foreach (KeyValuePair<string, string> entry in ai.Rulebased.TransitionBySystem)
            {
                if (!systems.Contains(entry.Key))
                {
                    errors.Add("ai.rulebased.transitionBySystem." + entry.Key + ": unknown system " + entry.Key);
                }
            }

            RequireKnown("ai.adaptive.passiveSystem", ai.Adaptive.PassiveSystem, systems, "system", errors);
            RequireKnown("ai.adaptive.aggressiveSystem", ai.Adaptive.AggressiveSystem, systems, "system", errors);
            return errors;
        }

        private static void RequireKnown(string path, string id, SortedSet<string> known, string kind, List<string> errors)
        {
            if (!known.Contains(id))
            {
                errors.Add(path + ": unknown " + kind + " " + id + " (known: " + string.Join(", ", known) + ")");
            }
        }

        private static void RequireUnit(string path, double value, List<string> errors)
        {
            if (!(value >= 0.0 && value <= 1.0))
            {
                errors.Add(path + ": must be in [0, 1], got " + Format(value));
            }
        }

        private static void RequirePeriodWindow(string path, double value, double periodSeconds, List<string> errors)
        {
            if (!(value > 0.0 && value <= periodSeconds))
            {
                errors.Add(path + ": must be > 0 and at most time.periodSeconds (" + Format(periodSeconds) + "), got " + Format(value));
            }
        }

        private static void RequireAtLeastOne(string path, int value, List<string> errors)
        {
            if (value < 1)
            {
                errors.Add(path + ": must be an integer >= 1, got " + value.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static bool Contains(IReadOnlyList<string> list, string value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Format(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
