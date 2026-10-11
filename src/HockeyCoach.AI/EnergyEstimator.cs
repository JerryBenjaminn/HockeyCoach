using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.AI
{
    /// <summary>
    /// The coach's estimate of its own skaters' energy from public information (data-schema.md AI-1): the clock, the
    /// line changes and timeouts in the public log of the current period. Every estimate is <c>energy.start</c> at the
    /// period start. On the ice t s: Ê −= (<c>energy.drainPerSecondOnIce</c> + <c>ai.estimate.extraDrainPerSecondOnIce</c>)
    /// × f × t (f the endurance factor of O-5). On the bench t s: Ê ← 1 − (1 − Ê) × exp(−<c>energy.benchRecoveryRate</c> × t).
    /// A timeout rests everyone as in AI-4. Clamped to 0–1.
    /// </summary>
    /// <remarks>
    /// Who is on the ice at the period start is read from the placements of the period's first event; line-change events
    /// then swap units. Between the last event and the view's time the view's own units are on the ice.
    /// </remarks>
    public sealed class EnergyEstimator
    {
        private readonly TuningConfig _tuning;

        /// <summary>Creates the estimator.</summary>
        public EnergyEstimator(TuningConfig tuning)
        {
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>Estimated energy of every own skater, by id, as of the view's time.</summary>
        public IReadOnlyDictionary<int, double> Estimate(CoachView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            var energy = new SortedDictionary<int, double>();
            var factor = new SortedDictionary<int, double>();
            double reference = _tuning.CheckFormula.ReferenceValue;
            foreach (Skater skater in view.Team.Skaters)
            {
                energy[skater.Id] = _tuning.Energy.Start;
                factor[skater.Id] = StateDynamics.ReductionFactor(_tuning.Energy.EnduranceCostReductionPerPoint, skater.Stats[SkaterStat.Endurance], reference);
            }

            var onIce = new SortedSet<int>();
            bool initialized = false;
            double last = 0.0;
            foreach (SimEvent e in view.Events)
            {
                if (e.Context.Period != view.Period)
                {
                    continue;
                }

                Advance(energy, factor, onIce, e.Context.Time - last);
                last = Math.Max(last, e.Context.Time);
                if (!initialized)
                {
                    foreach (PlayerPlacement placement in e.Context.Placement.Players)
                    {
                        if (!placement.IsGoalie && energy.ContainsKey(placement.PlayerId))
                        {
                            onIce.Add(placement.PlayerId);
                        }
                    }

                    initialized = true;
                }

                if (e is LineChangeEvent change)
                {
                    foreach (int id in change.OutgoingIds)
                    {
                        onIce.Remove(id);
                    }

                    foreach (int id in change.IncomingIds)
                    {
                        if (energy.ContainsKey(id))
                        {
                            onIce.Add(id);
                        }
                    }
                }
                else if (e is TimeoutEvent)
                {
                    var ids = new List<int>(energy.Keys);
                    foreach (int id in ids)
                    {
                        energy[id] = StateDynamics.BenchRecovered(energy[id], _tuning.Time.TimeoutRestSeconds, _tuning.Energy.BenchRecoveryRate);
                    }
                }
            }

            if (view.Time > last)
            {
                var current = new SortedSet<int>();
                if (view.ForwardIndex >= 0)
                {
                    foreach (Skater s in view.Team.ForwardLines[view.ForwardIndex].Skaters)
                    {
                        current.Add(s.Id);
                    }
                }

                if (view.PairIndex >= 0)
                {
                    foreach (Skater s in view.Team.DefencePairs[view.PairIndex].Skaters)
                    {
                        current.Add(s.Id);
                    }
                }

                Advance(energy, factor, initialized ? onIce : current, view.Time - last);
            }

            return energy;
        }

        /// <summary>Mean estimate of a unit's skaters.</summary>
        public static double UnitEnergy(IReadOnlyDictionary<int, double> estimate, IReadOnlyList<Skater> skaters)
        {
            double sum = 0.0;
            foreach (Skater s in skaters)
            {
                sum += estimate[s.Id];
            }

            return skaters.Count == 0 ? 0.0 : sum / skaters.Count;
        }

        private void Advance(SortedDictionary<int, double> energy, SortedDictionary<int, double> factor, SortedSet<int> onIce, double seconds)
        {
            if (seconds <= 0.0)
            {
                return;
            }

            double drain = _tuning.Energy.DrainPerSecondOnIce + _tuning.Ai.Estimate.ExtraDrainPerSecondOnIce;
            var ids = new List<int>(energy.Keys);
            foreach (int id in ids)
            {
                energy[id] = onIce.Contains(id)
                    ? StateDynamics.Clamp01(energy[id] - (drain * factor[id] * seconds))
                    : StateDynamics.BenchRecovered(energy[id], seconds, _tuning.Energy.BenchRecoveryRate);
            }
        }
    }
}
