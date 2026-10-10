using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Config
{
    /// <summary><c>ai.rulebased</c> (AI-2 S-L, S-T; AI-4).</summary>
    public sealed class AiRulebasedConfig
    {
        /// <summary>The mode keys of <c>modes</c>, in ordinal order.</summary>
        public static readonly IReadOnlyList<string> ModeKeys = new[] { "chase", "normal", "protect" };

        private readonly SortedDictionary<string, AiModeConfig> _modes;
        private readonly SortedDictionary<string, IReadOnlyDictionary<string, double>> _playBonusBySystem;

        /// <summary>Creates the config; the dictionaries are copied in ordinal order.</summary>
        public AiRulebasedConfig(
            double changeEnergyThreshold,
            double freshnessMargin,
            double lateGameSeconds,
            int protectLeadGoals,
            int chaseDeficitGoals,
            IEnumerable<KeyValuePair<string, AiModeConfig>> modes,
            IEnumerable<KeyValuePair<string, IReadOnlyDictionary<string, double>>> playBonusBySystem,
            AiTimeoutConfig timeout)
        {
            ChangeEnergyThreshold = changeEnergyThreshold;
            FreshnessMargin = freshnessMargin;
            LateGameSeconds = lateGameSeconds;
            ProtectLeadGoals = protectLeadGoals;
            ChaseDeficitGoals = chaseDeficitGoals;
            Timeout = timeout ?? throw new ArgumentNullException(nameof(timeout));
            if (modes == null)
            {
                throw new ArgumentNullException(nameof(modes));
            }

            if (playBonusBySystem == null)
            {
                throw new ArgumentNullException(nameof(playBonusBySystem));
            }

            _modes = new SortedDictionary<string, AiModeConfig>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, AiModeConfig> mode in modes)
            {
                _modes[mode.Key] = mode.Value ?? throw new ArgumentException("Null mode.", nameof(modes));
            }

            _playBonusBySystem = new SortedDictionary<string, IReadOnlyDictionary<string, double>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, IReadOnlyDictionary<string, double>> system in playBonusBySystem)
            {
                if (system.Value == null)
                {
                    throw new ArgumentException("Null play bonuses.", nameof(playBonusBySystem));
                }

                var bonuses = new SortedDictionary<string, double>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, double> bonus in system.Value)
                {
                    bonuses[bonus.Key] = bonus.Value;
                }

                _playBonusBySystem[system.Key] = bonuses;
            }
        }

        /// <summary>Change a unit whose estimated energy is below this.</summary>
        public double ChangeEnergyThreshold { get; }

        /// <summary>Change to a bench unit at least this much fresher.</summary>
        public double FreshnessMargin { get; }

        /// <summary>Late-game window of the last period, in game seconds left.</summary>
        public double LateGameSeconds { get; }

        /// <summary>Lead (goals) that switches to <c>protect</c> late.</summary>
        public int ProtectLeadGoals { get; }

        /// <summary>Deficit (goals) that switches to <c>chase</c> late.</summary>
        public int ChaseDeficitGoals { get; }

        /// <summary>Mode → tactics, in ordinal key order.</summary>
        public IReadOnlyDictionary<string, AiModeConfig> Modes
        {
            get { return _modes; }
        }

        /// <summary>Opponent system id → play id → bonus (logit), in ordinal key order.</summary>
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> PlayBonusBySystem
        {
            get { return _playBonusBySystem; }
        }

        /// <summary><c>ai.rulebased.timeout</c>.</summary>
        public AiTimeoutConfig Timeout { get; }
    }

    /// <summary>One entry of <c>ai.rulebased.modes</c>: the tactics of a game-state mode (AI-2 S-T).</summary>
    public sealed class AiModeConfig
    {
        /// <summary>The transition names a mode may use (O-7).</summary>
        public static readonly IReadOnlyList<string> TransitionNames = new[] { "rush", "regroup" };

        /// <summary>Creates the mode.</summary>
        public AiModeConfig(string system, string transition, bool netFrontAfterShot, int looseChasers, bool pinch)
        {
            System = system ?? throw new ArgumentNullException(nameof(system));
            Transition = transition ?? throw new ArgumentNullException(nameof(transition));
            NetFrontAfterShot = netFrontAfterShot;
            LooseChasers = looseChasers;
            Pinch = pinch;
        }

        /// <summary>Defensive system id.</summary>
        public string System { get; }

        /// <summary><c>rush</c> or <c>regroup</c>.</summary>
        public string Transition { get; }

        /// <summary>O-10 <c>netFrontAfterShot</c>.</summary>
        public bool NetFrontAfterShot { get; }

        /// <summary>O-10 <c>looseChasers</c> (1 or 2).</summary>
        public int LooseChasers { get; }

        /// <summary>O-10 <c>pinch</c>.</summary>
        public bool Pinch { get; }
    }

    /// <summary><c>ai.rulebased.timeout</c> (AI-4 rule-based trigger).</summary>
    public sealed class AiTimeoutConfig
    {
        /// <summary>Creates the config.</summary>
        public AiTimeoutConfig(double tiredEnergy, double lateGameSeconds, int minDeficitGoals, int maxDeficitGoals)
        {
            TiredEnergy = tiredEnergy;
            LateGameSeconds = lateGameSeconds;
            MinDeficitGoals = minDeficitGoals;
            MaxDeficitGoals = maxDeficitGoals;
        }

        /// <summary>A stuck unit below this estimated energy triggers a timeout.</summary>
        public double TiredEnergy { get; }

        /// <summary>Late-game window for the chasing timeout, in game seconds left.</summary>
        public double LateGameSeconds { get; }

        /// <summary>Smallest deficit for the chasing timeout.</summary>
        public int MinDeficitGoals { get; }

        /// <summary>Largest deficit for the chasing timeout.</summary>
        public int MaxDeficitGoals { get; }
    }

    /// <summary><c>ai.adaptive</c> (AI-2 A-T).</summary>
    public sealed class AiAdaptiveConfig
    {
        /// <summary>Creates the config.</summary>
        public AiAdaptiveConfig(
            double windowSeconds,
            double priorWeight,
            string passiveSystem,
            string aggressiveSystem,
            double controlledEntryShareForPassive,
            double controlledEntryShareForAggressive)
        {
            WindowSeconds = windowSeconds;
            PriorWeight = priorWeight;
            PassiveSystem = passiveSystem ?? throw new ArgumentNullException(nameof(passiveSystem));
            AggressiveSystem = aggressiveSystem ?? throw new ArgumentNullException(nameof(aggressiveSystem));
            ControlledEntryShareForPassive = controlledEntryShareForPassive;
            ControlledEntryShareForAggressive = controlledEntryShareForAggressive;
        }

        /// <summary>Observation window in match seconds.</summary>
        public double WindowSeconds { get; }

        /// <summary>Prior weight in pseudo-events.</summary>
        public double PriorWeight { get; }

        /// <summary>System against a carrying opponent.</summary>
        public string PassiveSystem { get; }

        /// <summary>System against a dumping opponent.</summary>
        public string AggressiveSystem { get; }

        /// <summary>Controlled-entry share at or above which the passive system is used.</summary>
        public double ControlledEntryShareForPassive { get; }

        /// <summary>Controlled-entry share at or below which the aggressive system is used.</summary>
        public double ControlledEntryShareForAggressive { get; }
    }
}
