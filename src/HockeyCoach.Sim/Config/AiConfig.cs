using System;

namespace HockeyCoach.Sim.Config
{
    /// <summary>tuning.json <c>ai</c>: coach AI thresholds (data-schema.md AI-3, D-077). Use <see cref="AiConfigValidator"/> to check it.</summary>
    public sealed class AiConfig
    {
        /// <summary>Creates the config.</summary>
        public AiConfig(AiRandomConfig random, AiEstimateConfig estimate, AiRulebasedConfig rulebased, AiAdaptiveConfig adaptive)
        {
            Random = random ?? throw new ArgumentNullException(nameof(random));
            Estimate = estimate ?? throw new ArgumentNullException(nameof(estimate));
            Rulebased = rulebased ?? throw new ArgumentNullException(nameof(rulebased));
            Adaptive = adaptive ?? throw new ArgumentNullException(nameof(adaptive));
        }

        /// <summary><c>ai.random</c>.</summary>
        public AiRandomConfig Random { get; }

        /// <summary><c>ai.estimate</c>.</summary>
        public AiEstimateConfig Estimate { get; }

        /// <summary><c>ai.rulebased</c>.</summary>
        public AiRulebasedConfig Rulebased { get; }

        /// <summary><c>ai.adaptive</c>.</summary>
        public AiAdaptiveConfig Adaptive { get; }
    }

    /// <summary><c>ai.random</c> (AI-2 R-L, R-T).</summary>
    public sealed class AiRandomConfig
    {
        /// <summary>Creates the config.</summary>
        /// <param name="onTheFlyChangeProbability">Chance to change an eligible unit type at an on-the-fly opportunity.</param>
        /// <param name="timeoutProbabilityPerStoppage">Chance to call an available timeout at an allowed stoppage.</param>
        public AiRandomConfig(double onTheFlyChangeProbability, double timeoutProbabilityPerStoppage)
        {
            OnTheFlyChangeProbability = onTheFlyChangeProbability;
            TimeoutProbabilityPerStoppage = timeoutProbabilityPerStoppage;
        }

        /// <summary>Chance to change an eligible unit type at an on-the-fly opportunity.</summary>
        public double OnTheFlyChangeProbability { get; }

        /// <summary>Chance to call an available timeout at an allowed stoppage.</summary>
        public double TimeoutProbabilityPerStoppage { get; }
    }

    /// <summary><c>ai.estimate</c> (AI-1 energy estimate).</summary>
    public sealed class AiEstimateConfig
    {
        /// <summary>Creates the config.</summary>
        /// <param name="extraDrainPerSecondOnIce">Extra estimated drain per game second on the ice.</param>
        public AiEstimateConfig(double extraDrainPerSecondOnIce)
        {
            ExtraDrainPerSecondOnIce = extraDrainPerSecondOnIce;
        }

        /// <summary>Extra estimated drain per game second on the ice.</summary>
        public double ExtraDrainPerSecondOnIce { get; }
    }
}
