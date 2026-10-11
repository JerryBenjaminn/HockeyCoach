using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.AI
{
    /// <summary>The game-state mode of the rule-based tactics (AI-2 S-T step 1).</summary>
    public enum GameMode
    {
        /// <summary><c>normal</c>.</summary>
        Normal = 0,

        /// <summary><c>protect</c>: leading late.</summary>
        Protect = 1,

        /// <summary><c>chase</c>: trailing late.</summary>
        Chase = 2,
    }

    /// <summary>
    /// Rule-based tactics (data-schema.md AI-2, S-T) and the rule-based timeout trigger (AI-4). Deterministic: the same
    /// public state gives the same decision.
    /// </summary>
    public class RuleTactics : ITacticsPolicy
    {
        private readonly Play[] _library;
        private readonly SortedDictionary<string, DefensiveSystem> _systems;
        private readonly TuningConfig _tuning;
        private readonly EnergyEstimator _estimator;

        /// <summary>Creates the policy.</summary>
        /// <param name="library">All plays in load order.</param>
        /// <param name="systems">All loaded systems.</param>
        /// <param name="tuning">Tuning (ai, familiarity, energy, time).</param>
        public RuleTactics(IReadOnlyList<Play> library, IReadOnlyList<DefensiveSystem> systems, TuningConfig tuning)
        {
            if (library == null || systems == null)
            {
                throw new ArgumentNullException(library == null ? nameof(library) : nameof(systems));
            }

            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            _library = new Play[library.Count];
            for (int i = 0; i < library.Count; i++)
            {
                _library[i] = library[i] ?? throw new ArgumentException("Null play.", nameof(library));
            }

            _systems = new SortedDictionary<string, DefensiveSystem>(StringComparer.Ordinal);
            foreach (DefensiveSystem system in systems)
            {
                _systems[system.Id] = system;
            }

            _estimator = new EnergyEstimator(tuning);
        }

        /// <summary>The loaded systems by id, in ordinal order.</summary>
        protected IReadOnlyDictionary<string, DefensiveSystem> Systems
        {
            get { return _systems; }
        }

        /// <summary>Tuning.</summary>
        protected TuningConfig Tuning
        {
            get { return _tuning; }
        }

        /// <summary>The mode key of <c>ai.rulebased.modes</c>.</summary>
        public static string ModeKey(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Protect:
                    return "protect";
                case GameMode.Chase:
                    return "chase";
                default:
                    return "normal";
            }
        }

        /// <summary>S-T step 1: last period with at most <c>lateGameSeconds</c> left: lead → protect, deficit → chase.</summary>
        public GameMode Mode(CoachView view)
        {
            AiRulebasedConfig rule = _tuning.Ai.Rulebased;
            if (view.Clock.Period < view.Clock.Periods || view.Clock.SecondsLeftInPeriod > rule.LateGameSeconds)
            {
                return GameMode.Normal;
            }

            int lead = view.OwnGoals - view.OpponentGoals;
            if (lead >= rule.ProtectLeadGoals)
            {
                return GameMode.Protect;
            }

            return lead <= -rule.ChaseDeficitGoals ? GameMode.Chase : GameMode.Normal;
        }

        /// <inheritdoc />
        public bool Timeout(CoachView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (!view.Timeouts.CanCall)
            {
                return false;
            }

            AiTimeoutConfig timeout = _tuning.Ai.Rulebased.Timeout;
            IReadOnlyDictionary<int, double> estimate = null;
            if (!view.CanChangeForwards && view.ForwardIndex >= 0)
            {
                estimate = _estimator.Estimate(view);
                if (EnergyEstimator.UnitEnergy(estimate, view.Team.ForwardLines[view.ForwardIndex].Skaters) < timeout.TiredEnergy)
                {
                    return true;
                }
            }

            if (!view.CanChangeDefence && view.PairIndex >= 0)
            {
                estimate = estimate ?? _estimator.Estimate(view);
                if (EnergyEstimator.UnitEnergy(estimate, view.Team.DefencePairs[view.PairIndex].Skaters) < timeout.TiredEnergy)
                {
                    return true;
                }
            }

            int deficit = view.OpponentGoals - view.OwnGoals;
            return view.Clock.Period == view.Clock.Periods
                && view.Clock.SecondsLeftInPeriod <= timeout.LateGameSeconds
                && deficit >= timeout.MinDeficitGoals
                && deficit <= timeout.MaxDeficitGoals;
        }

        /// <inheritdoc />
        public ShiftPlan Plan(CoachView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            GameMode mode = Mode(view);
            AiModeConfig settings = _tuning.Ai.Rulebased.Modes[ModeKey(mode)];
            string system = mode == GameMode.Normal ? NormalSystem(view, settings.System) : settings.System;
            return new ShiftPlan(
                Playbook(view, PlayBonuses(view)),
                _systems[system],
                settings.Transition == "regroup" ? TransitionInstruction.Regroup : TransitionInstruction.Rush,
                new SystemModeInstructions(settings.NetFrontAfterShot, settings.LooseChasers, settings.Pinch));
        }

        /// <summary>The own system in mode <c>normal</c>; S-T uses <c>modes.normal.system</c>.</summary>
        protected virtual string NormalSystem(CoachView view, string configured)
        {
            return configured;
        }

        /// <summary>
        /// Bonus per play id against the opponent (S-T step 2–3: the last observed opponent system; missing = 0).
        /// </summary>
        protected virtual IReadOnlyDictionary<string, double> PlayBonuses(CoachView view)
        {
            string system = PublicLog.LastOpponentSystem(view);
            return system != null && _tuning.Ai.Rulebased.PlayBonusBySystem.TryGetValue(system, out IReadOnlyDictionary<string, double> bonuses)
                ? bonuses
                : new SortedDictionary<string, double>(StringComparer.Ordinal);
        }

        /// <summary>
        /// S-T step 3: B(k) = bonus(k) − L(u_k + 1) with the O-11 penalty L; order by larger B, then fewer uses, then load
        /// order. The whole library.
        /// </summary>
        public IReadOnlyList<Play> Playbook(CoachView view, IReadOnlyDictionary<string, double> bonuses)
        {
            FamiliarityConfig f = _tuning.Familiarity;
            var score = new double[_library.Length];
            var uses = new double[_library.Length];
            var order = new List<int>();
            for (int i = 0; i < _library.Length; i++)
            {
                uses[i] = view.PlayUses.TryGetValue(_library[i].Id, out double u) ? u : 0.0;
                double bonus = bonuses.TryGetValue(_library[i].Id, out double b) ? b : 0.0;
                score[i] = bonus - StateDynamics.FamiliarityPenalty(uses[i] + 1.0, f.FreeUses, f.PenaltyPerRepeat, f.MaxPenalty);
                order.Add(i);
            }

            order.Sort((a, b) =>
            {
                int c = score[b].CompareTo(score[a]);
                if (c != 0)
                {
                    return c;
                }

                c = uses[a].CompareTo(uses[b]);
                return c != 0 ? c : a.CompareTo(b);
            });
            var plays = new List<Play>();
            foreach (int i in order)
            {
                plays.Add(_library[i]);
            }

            return plays;
        }
    }
}
