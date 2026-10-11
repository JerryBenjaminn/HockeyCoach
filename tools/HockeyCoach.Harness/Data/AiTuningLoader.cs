using HockeyCoach.Sim.Config;

namespace HockeyCoach.Harness.Data;

/// <summary>
/// Maps tuning.json <c>ai</c> (data-schema.md AI-3, AI-6) into <see cref="AiConfig"/>. Every key is required and unknown
/// keys are rejected. Missing or mistyped values are recorded as errors on the reader; the caller discards the result
/// when any error was recorded.
/// </summary>
internal static class AiTuningLoader
{
    /// <summary>Maps the section, or returns null if it is absent.</summary>
    public static AiConfig? Map(JsonReader? reader)
    {
        if (reader == null)
        {
            return null;
        }

        reader.RejectUnknown("random", "estimate", "rulebased", "adaptive");
        AiRandomConfig random = MapRandom(reader.Required("random"));
        AiEstimateConfig estimate = MapEstimate(reader.Required("estimate"));
        AiRulebasedConfig rulebased = MapRulebased(reader.Required("rulebased"));
        AiAdaptiveConfig adaptive = MapAdaptive(reader.Required("adaptive"));
        return new AiConfig(random, estimate, rulebased, adaptive);
    }

    private static AiRandomConfig MapRandom(JsonReader? reader)
    {
        if (reader == null)
        {
            return new AiRandomConfig(double.NaN, double.NaN);
        }

        reader.RejectUnknown("onTheFlyChangeProbability", "timeoutProbabilityPerStoppage");
        return new AiRandomConfig(reader.Double("onTheFlyChangeProbability"), reader.Double("timeoutProbabilityPerStoppage"));
    }

    private static AiEstimateConfig MapEstimate(JsonReader? reader)
    {
        if (reader == null)
        {
            return new AiEstimateConfig(double.NaN);
        }

        reader.RejectUnknown("extraDrainPerSecondOnIce");
        return new AiEstimateConfig(reader.Double("extraDrainPerSecondOnIce"));
    }

    private static AiRulebasedConfig MapRulebased(JsonReader? reader)
    {
        var modes = new List<KeyValuePair<string, AiModeConfig>>();
        var bonuses = new List<KeyValuePair<string, IReadOnlyDictionary<string, double>>>();
        var transitions = new List<KeyValuePair<string, string>>();
        if (reader == null)
        {
            return new AiRulebasedConfig(double.NaN, double.NaN, double.NaN, 0, 0, modes, bonuses, transitions, MapTimeout(null));
        }

        reader.RejectUnknown(
            "changeEnergyThreshold", "freshnessMargin", "lateGameSeconds", "protectLeadGoals", "chaseDeficitGoals", "modes", "playBonusBySystem",
            "transitionBySystem", "timeout");
        JsonReader? modesReader = reader.Required("modes");
        if (modesReader != null)
        {
            foreach (KeyValuePair<string, JsonReader> mode in modesReader.Properties())
            {
                modes.Add(new KeyValuePair<string, AiModeConfig>(mode.Key, MapMode(mode.Value)));
            }
        }

        JsonReader? bonusReader = reader.Required("playBonusBySystem");
        if (bonusReader != null)
        {
            foreach (KeyValuePair<string, JsonReader> system in bonusReader.Properties())
            {
                var plays = new SortedDictionary<string, double>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, JsonReader> play in system.Value.Properties())
                {
                    plays[play.Key] = play.Value.AsDouble();
                }

                bonuses.Add(new KeyValuePair<string, IReadOnlyDictionary<string, double>>(system.Key, plays));
            }
        }

        JsonReader? transitionReader = reader.Required("transitionBySystem");
        if (transitionReader != null)
        {
            foreach (KeyValuePair<string, JsonReader> system in transitionReader.Properties())
            {
                transitions.Add(new KeyValuePair<string, string>(system.Key, system.Value.AsString() ?? string.Empty));
            }
        }

        return new AiRulebasedConfig(
            reader.Double("changeEnergyThreshold"),
            reader.Double("freshnessMargin"),
            reader.Double("lateGameSeconds"),
            reader.Int("protectLeadGoals") ?? 0,
            reader.Int("chaseDeficitGoals") ?? 0,
            modes,
            bonuses,
            transitions,
            MapTimeout(reader.Required("timeout")));
    }

    private static AiModeConfig MapMode(JsonReader reader)
    {
        reader.RejectUnknown("system", "transition", "netFrontAfterShot", "looseChasers", "pinch");
        return new AiModeConfig(
            reader.String("system") ?? string.Empty,
            reader.String("transition") ?? string.Empty,
            Bool(reader, "netFrontAfterShot"),
            reader.Int("looseChasers") ?? 0,
            Bool(reader, "pinch"));
    }

    private static AiTimeoutConfig MapTimeout(JsonReader? reader)
    {
        if (reader == null)
        {
            return new AiTimeoutConfig(double.NaN, double.NaN, 0, 0);
        }

        reader.RejectUnknown("tiredEnergy", "lateGameSeconds", "minDeficitGoals", "maxDeficitGoals");
        return new AiTimeoutConfig(
            reader.Double("tiredEnergy"),
            reader.Double("lateGameSeconds"),
            reader.Int("minDeficitGoals") ?? 0,
            reader.Int("maxDeficitGoals") ?? 0);
    }

    private static AiAdaptiveConfig MapAdaptive(JsonReader? reader)
    {
        if (reader == null)
        {
            return new AiAdaptiveConfig(double.NaN, double.NaN, string.Empty, string.Empty, 0, 0, double.NaN, double.NaN);
        }

        reader.RejectUnknown(
            "windowSeconds", "priorWeight", "passiveSystem", "aggressiveSystem", "styleWindowEvents", "styleMinEvents", "rushShareForPassive", "rushShareForAggressive");
        return new AiAdaptiveConfig(
            reader.Double("windowSeconds"),
            reader.Double("priorWeight"),
            reader.String("passiveSystem") ?? string.Empty,
            reader.String("aggressiveSystem") ?? string.Empty,
            reader.Int("styleWindowEvents") ?? 0,
            reader.Int("styleMinEvents") ?? 0,
            reader.Double("rushShareForPassive"),
            reader.Double("rushShareForAggressive"));
    }

    private static bool Bool(JsonReader reader, string name) => reader.Required(name)?.AsBool() ?? false;
}
