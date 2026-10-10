using System.Collections.Generic;
using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// Loose pucks (M-6, Q-007) and the dump (E-001, D-031). The first battle at a new loose puck is resolved in the next
    /// step without movement; after "no winner" both teams take one system step before the battle is retried.
    /// </summary>
    internal sealed partial class ShiftRun
    {
        private TeamSide _battleAttacker;
        private LooseKind _looseKind;
        private Skater _dumper;
        private bool _battleRetry;
        private string _dumpPlayId;

        private enum LooseKind
        {
            Normal,
            ReboundSlot,
            Dump,
        }

        private void StartLooseBattle(TeamSide attackerSide, LooseKind kind, Skater dumper)
        {
            _dumpPlayId = kind == LooseKind.Dump ? _play?.Id : null;
            _play = null;
            SetAttacker(attackerSide);
            _battleAttacker = attackerSide;
            _looseKind = kind;
            _dumper = dumper;
            _battleRetry = false;
            _mode = Mode.LooseBattle;
        }

        /// <summary><c>dump</c>: the puck flies to the target node, then the loose-puck battle there (no check on the way).</summary>
        private void Dump(TeamSide team, Position shooter, GridPoint target)
        {
            _time += _tuning.Time.GetSecondsPerAction("dumpIn");
            ClearActionHistory();
            Skater dumper = _state.SkaterAt(team, shooter);
            _state.SetLoosePuck(TeamFrame.ToRink(target, team, _rink));
            StartLooseBattle(team, LooseKind.Dump, dumper);
        }

        private void LooseBattleStep()
        {
            TeamSide attacker = _battleAttacker;
            TeamSide defender = TeamSides.Opponent(attacker);
            if (_battleRetry)
            {
                // M-6.5: both teams take one system step around the loose puck (Q-035 default: each by its own system).
                _time += _tuning.Time.GetSecondsPerAction("systemStep");
                _state.ApplySystem(attacker, Team(attacker).Plan.System, _tuning.Plays.MaxNodesPerBeat);
                _state.ApplySystem(defender, Team(defender).Plan.System, _tuning.Plays.MaxNodesPerBeat);
            }

            GridPoint spot = _state.PuckNode;
            Position attackerPosition = NearestSkater(attacker, spot);
            Position defenderPosition = NearestSkater(defender, spot);
            Skater a = _state.SkaterAt(attacker, attackerPosition);
            Skater d = _state.SkaterAt(defender, defenderPosition);
            int attackerDistance = GridPoint.Chebyshev(_state.NodeOf(attacker, attackerPosition), spot);
            int defenderDistance = GridPoint.Chebyshev(_state.NodeOf(defender, defenderPosition), spot);

            CheckDefinition check = _tuning.GetCheck("loosePuck");
            double m = check.Modifiers.Get("distancePerNode") * (defenderDistance - attackerDistance)
                + ExtraPlayer(check, attacker, defender, spot)
                + OffSide(new[] { a }, attacker, new[] { d }, defender);
            if (_looseKind == LooseKind.Dump && !_battleRetry)
            {
                m += GoalieReach(attacker, spot);
            }

            BattleOutcome outcome;
            if (_random.Chance(check.GetParameter("noWinnerShare")))
            {
                outcome = BattleOutcome.NoWinner;
            }
            else
            {
                CheckResult result = SidedCheck.Resolve(
                    _tuning.CheckFormula,
                    check,
                    new CheckParticipants().With("participant", a),
                    new CheckParticipants().With("participant", d),
                    m,
                    _random);
                outcome = result.Success ? BattleOutcome.Win : BattleOutcome.Loss;
            }

            _time += _tuning.Time.GetSecondsPerAction("loosePuck");
            ClearActionHistory();
            if (outcome != BattleOutcome.NoWinner)
            {
                TeamSide winner = outcome == BattleOutcome.Win ? attacker : defender;
                Position winnerPosition = outcome == BattleOutcome.Win ? attackerPosition : defenderPosition;
                _state.Place(winner, winnerPosition, spot);
                _state.GivePuckTo(winner, winnerPosition);
            }

            if (_looseKind == LooseKind.Dump && !_battleRetry)
            {
                // The dump belongs to the play that made it (D-045).
                _log.Append(new DumpInEvent(Context(_dumpPlayId, Team(defender).Plan.System.Id), _dumper.Id, outcome));
            }
            else
            {
                _log.Append(new PuckBattleEvent(Context(), new[] { a.Id }, new[] { d.Id }, outcome));
            }

            if (outcome == BattleOutcome.NoWinner)
            {
                _battleRetry = true;
                return;
            }

            bool reboundWin = _looseKind == LooseKind.ReboundSlot && outcome == BattleOutcome.Win;
            TeamSide gainer = outcome == BattleOutcome.Win ? attacker : defender;
            GainPossession(gainer, outcome == BattleOutcome.Win ? attackerPosition : defenderPosition);
            if (reboundWin && _mode == Mode.PlayBeat && _play != null && _beat == 0)
            {
                // Setup moved the players; the rebound condition of D-047 only holds while the winner still stands at the spot.
                _hasReboundWinner = _state.NodeOf(attacker, attackerPosition).Equals(spot);
                _reboundTeam = attacker;
                _reboundPosition = attackerPosition;
            }
        }

        /// <summary>The team's skater nearest the node: Chebyshev, then Manhattan, then position order.</summary>
        private Position NearestSkater(TeamSide team, GridPoint node)
        {
            var nodes = new SortedDictionary<Position, GridPoint>();
            foreach (Position position in PositionOrder.All)
            {
                nodes.Add(position, _state.NodeOf(team, position));
            }

            LineGeometry.Closest(nodes, new[] { node }, node, int.MaxValue, out Position nearest, out int unused);
            return nearest;
        }

        /// <summary>M-6.4: once, for the side with more skaters within <c>extraPlayerRadius</c>.</summary>
        private double ExtraPlayer(CheckDefinition check, TeamSide attacker, TeamSide defender, GridPoint spot)
        {
            int radius = (int)check.GetParameter("extraPlayerRadius");
            int attackers = 0;
            int defenders = 0;
            foreach (Position position in PositionOrder.All)
            {
                if (GridPoint.Chebyshev(_state.NodeOf(attacker, position), spot) <= radius)
                {
                    attackers++;
                }

                if (GridPoint.Chebyshev(_state.NodeOf(defender, position), spot) <= radius)
                {
                    defenders++;
                }
            }

            double value = check.Modifiers.Get("extraPlayer");
            return attackers > defenders ? value : (defenders > attackers ? -value : 0.0);
        }

        /// <summary>
        /// <c>checks.dumpIn</c>: within <c>goalieReachNodes</c> of the defending goal, the goalie's puck handling above the
        /// reference value favours the defending team.
        /// </summary>
        private double GoalieReach(TeamSide dumpingTeam, GridPoint spot)
        {
            CheckDefinition dumpIn = _tuning.GetCheck("dumpIn");
            GridPoint target = TeamFrame.ToTeamView(spot, dumpingTeam, _rink);
            if (GridPoint.Chebyshev(target, _rink.OpponentGoal) > (int)dumpIn.GetParameter("goalieReachNodes"))
            {
                return 0.0;
            }

            Goalie goalie = Team(TeamSides.Opponent(dumpingTeam)).Goalie;
            return -dumpIn.GetParameter("goaliePuckHandlingPerPoint") * (goalie.Stats[GoalieStat.PuckHandling] - _tuning.CheckFormula.ReferenceValue);
        }
    }
}
