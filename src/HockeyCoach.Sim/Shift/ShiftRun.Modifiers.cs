using System.Collections.Generic;
using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// State modifiers of the checks (energy O-5, organization O-6, familiarity O-11, pressure O-9), the participation
    /// cost (O-5) and the commitments of the system-mode instructions (O-10).
    /// </summary>
    internal sealed partial class ShiftRun
    {
        /// <summary>Organization (O-6) plus energy (O-5) for a check with both sides' participants.</summary>
        private double StateModifier(CheckModifiers modifiers, TeamSide defending, IEnumerable<Skater> attackerSide, IEnumerable<Skater> defenderSide)
        {
            return OrganizationModifier(modifiers, defending) + EnergyModifier(Ids(attackerSide), Ids(defenderSide));
        }

        /// <summary>O-6: <c>modifiers.organization</c> × (1 − O_defending), or 0 for a check without it.</summary>
        private double OrganizationModifier(CheckModifiers modifiers, TeamSide defending)
        {
            return modifiers.Scalars.TryGetValue(TuningValidator.OrganizationModifier, out double coefficient)
                ? StateDynamics.OrganizationModifier(coefficient, _game.Organization(defending))
                : 0.0;
        }

        /// <summary>O-5: cz × (1 − Ē_attacker) − cz × (1 − Ē_defender); an empty side contributes 0.</summary>
        private double EnergyModifier(IReadOnlyList<int> attackerIds, IReadOnlyList<int> defenderIds)
        {
            return StateDynamics.EnergyModifier(_tuning.Energy.CheckModifierAtZero, MeanEnergy(attackerIds), MeanEnergy(defenderIds));
        }

        private double MeanEnergy(IReadOnlyList<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return double.NaN;
            }

            double sum = 0.0;
            foreach (int id in ids)
            {
                sum += _game.Energy.Get(id);
            }

            return sum / ids.Count;
        }

        private static int[] Ids(IEnumerable<Skater> skaters)
        {
            var ids = new List<int>();
            foreach (Skater skater in skaters)
            {
                ids.Add(skater.Id);
            }

            return ids.ToArray();
        }

        /// <summary>O-11: the running play's familiarity penalty, for its passes, carries and block; 0 outside a play.</summary>
        private double PlayFamiliarity()
        {
            return _play != null ? _familiarityPenalty : 0.0;
        }

        /// <summary>O-5: every skater who took part in a check pays <c>energy.costPerAction</c> × f after the check.</summary>
        private void PayCost(params Skater[] skaters)
        {
            foreach (Skater skater in skaters)
            {
                double f = StateDynamics.ReductionFactor(
                    _tuning.Energy.EnduranceCostReductionPerPoint, skater.Stats[SkaterStat.Endurance], _tuning.CheckFormula.ReferenceValue);
                _game.Energy.Set(skater.Id, _game.Energy.Get(skater.Id) - (_tuning.Energy.CostPerAction * f));
            }
        }

        /// <summary>O-9: an event gain of the team's pressure state.</summary>
        private void GainPressure(TeamSide team, double gain)
        {
            _game.SetPressure(team, _game.Pressure(team) + gain);
        }

        /// <summary>O-10: committed players of the team (instruction movers and a pinching D1).</summary>
        private int Committed(TeamSide team)
        {
            int count = 0;
            for (int position = 0; position < 5; position++)
            {
                if (_committed[(int)team, position])
                {
                    count++;
                }
            }

            if (_pinchActive[(int)team] && !_committed[(int)team, (int)PinchingDefender(team)])
            {
                count++;
            }

            return count;
        }

        /// <summary>The team's D1 (closer defenceman to the puck), who pinches (O-10).</summary>
        private Position PinchingDefender(TeamSide team)
        {
            GridPoint puck = Tactics.TeamFrame.ToTeamView(_state.PuckNode, team, _rink);
            IReadOnlyDictionary<Tactics.SystemRole, Position> roles = Tactics.RoleAssigner.Assign(_state.NodesInTeamView(team), puck, _rink);
            return roles[Tactics.SystemRole.D1];
        }

        /// <summary>O-10: commitments end when the team's next setup starts (and at every stoppage, i.e. with the segment).</summary>
        private void ClearCommitments(TeamSide team)
        {
            for (int position = 0; position < 5; position++)
            {
                _committed[(int)team, position] = false;
            }
        }
    }
}
