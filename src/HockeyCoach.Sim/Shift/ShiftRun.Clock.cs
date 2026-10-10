using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.State;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// The clock. Every atomic sequence of O-1 starts with <see cref="Begin(double)"/>: if it would cross the end of the period,
    /// nothing of it happens and the period ends (D-064). Otherwise the per-second rates are captured from the state
    /// before the sequence (O-14) and every <see cref="Spend(double)"/> of the sequence applies them, in the fixed order
    /// energy → organization → pressure → ice time and offensive-zone time, home team first, players in id order.
    /// <see cref="Spend(double)"/> is the only place where game time moves.
    /// </summary>
    internal sealed partial class ShiftRun
    {
        /// <summary>Numeric tolerance for comparing sums of seconds (not a balance value).</summary>
        private const double TimeEpsilon = 1e-9;

        private readonly SortedDictionary<int, double> _drainPerSecond = new SortedDictionary<int, double>();
        private readonly double[] _organizationRate = new double[2];
        private readonly double[] _pressureRate = new double[2];
        private readonly bool[] _puckInOffensiveZone = new bool[2];

        /// <summary>Whether a sequence of <paramref name="duration"/> seconds would end after the period.</summary>
        private bool WouldCross(double duration)
        {
            return _time + duration > _tuning.Time.PeriodSeconds + TimeEpsilon;
        }

        /// <summary>Starts a sequence: false (and the period ends) if it would cross the period end.</summary>
        private bool Begin(double duration)
        {
            if (WouldCross(duration))
            {
                EndPeriod();
                return false;
            }

            CaptureRates();
            return true;
        }

        /// <summary>Starts a single-action sequence (<c>time.secondsPerAction.&lt;key&gt;</c>).</summary>
        private bool Begin(string actionKey)
        {
            return Begin(Seconds(actionKey));
        }

        private double Seconds(string actionKey)
        {
            return _tuning.Time.GetSecondsPerAction(actionKey);
        }

        /// <summary>Spends the time of one action (<c>time.secondsPerAction.&lt;key&gt;</c>).</summary>
        private void Spend(string actionKey)
        {
            Spend(Seconds(actionKey));
        }

        /// <summary>Advances the clock by <paramref name="seconds"/> and applies the captured per-second effects.</summary>
        private void Spend(double seconds)
        {
            ApplyRates(seconds);
            double before = _time;
            _time += seconds;
            SamplePressure(before, _time);
        }

        /// <summary>O-1: the clock goes to the period end, the remaining time counts for the states, and the stoppage is logged.</summary>
        private void EndPeriod()
        {
            CaptureRates();
            double remaining = Math.Max(0.0, _tuning.Time.PeriodSeconds - _time);
            ApplyRates(remaining);
            double before = _time;
            _time = _tuning.Time.PeriodSeconds;
            SamplePressure(before, _time);
            End(ShiftEndReason.PeriodEnd, StoppageReason.PeriodEnd);
        }

        private void SamplePressure(double before, double after)
        {
            double step = GameState.PressureSampleSeconds;
            for (double t = (Math.Floor(before / step) + 1.0) * step; t <= after + TimeEpsilon; t += step)
            {
                _game.SamplePressure(_setup.Period, t);
            }
        }

        private void CaptureRates()
        {
            _drainPerSecond.Clear();
            double reference = _tuning.CheckFormula.ReferenceValue;
            foreach (TeamSide team in new[] { TeamSide.Home, TeamSide.Away })
            {
                double opponentPressure = _game.Pressure(TeamSides.Opponent(team));
                OnIceSkaters unit = _state.UnitOf(team);
                double statSum = 0.0;
                foreach (Skater skater in unit.Skaters)
                {
                    double f = StateDynamics.ReductionFactor(_tuning.Energy.EnduranceCostReductionPerPoint, skater.Stats[SkaterStat.Endurance], reference);
                    _drainPerSecond[skater.Id] = (_tuning.Energy.DrainPerSecondOnIce + (_tuning.Pressure.EnergyDrainPerSecond * opponentPressure)) * f;
                    foreach (KeyValuePair<SkaterStat, double> weight in _tuning.Organization.RecoveryWeights)
                    {
                        statSum += weight.Value * skater.Stats[weight.Key];
                    }
                }

                Goalie goalie = GoalieOf(team);
                _drainPerSecond[goalie.Id] = _tuning.Pressure.EnergyDrainPerSecond * opponentPressure * GoalieFactor(goalie);
                double meanStat = statSum / unit.Skaters.Count;
                _organizationRate[(int)team] = StateDynamics.OrganizationRecoveryPerSecond(
                    _tuning.Organization.RecoveryPerSecond, _tuning.Organization.RecoveryPerStatPoint, meanStat, reference);

                bool inZone = ZoneFor(team, _state.PuckNode) == RinkZone.Offensive;
                bool holds = _state.HasCarrier && _state.CarrierTeam == team;
                _puckInOffensiveZone[(int)team] = inZone;
                _pressureRate[(int)team] = inZone && holds ? _tuning.Pressure.GainPerSecondInZone : -_tuning.Pressure.DecayPerSecond;
            }
        }

        private void ApplyRates(double seconds)
        {
            if (seconds <= 0.0)
            {
                return;
            }

            foreach (KeyValuePair<int, double> drain in _drainPerSecond)
            {
                _game.Energy.Set(drain.Key, _game.Energy.Get(drain.Key) - (drain.Value * seconds));
            }

            foreach (TeamSide team in new[] { TeamSide.Home, TeamSide.Away })
            {
                _game.SetOrganization(team, _game.Organization(team) + (_organizationRate[(int)team] * seconds));
            }

            foreach (TeamSide team in new[] { TeamSide.Home, TeamSide.Away })
            {
                _game.SetPressure(team, _game.Pressure(team) + (_pressureRate[(int)team] * seconds));
            }

            foreach (TeamSide team in new[] { TeamSide.Home, TeamSide.Away })
            {
                _game.Lineup(team).AddIceTime(seconds);
                if (_puckInOffensiveZone[(int)team])
                {
                    _game.AddOffensiveZoneSeconds(team, seconds);
                }
            }
        }

        /// <summary>The goalie's mental toughness factor g (O-5, O-9).</summary>
        private double GoalieFactor(Goalie goalie)
        {
            return StateDynamics.ReductionFactor(
                _tuning.Pressure.MentalToughnessReductionPerPoint, goalie.Stats[GoalieStat.MentalToughness], _tuning.CheckFormula.ReferenceValue);
        }
    }
}
