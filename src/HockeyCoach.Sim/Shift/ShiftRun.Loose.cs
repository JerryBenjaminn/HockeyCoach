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
    /// step without movement; after "no winner" both teams take one system step before the battle is retried. A win by
    /// the attacking side in its offensive slot is an immediate second-chance shot (D-059, D-065, O-8).
    /// </summary>
    internal sealed partial class ShiftRun
    {
        private TeamSide _battleAttacker;
        private LooseKind _looseKind;
        private Skater _dumper;
        private bool _battleRetry;
        private bool _dumpPrepaid;
        private string _dumpPlayId;

        private enum LooseKind
        {
            Normal,
            Dump,
        }

        private void StartLooseBattle(TeamSide attackerSide, LooseKind kind, Skater dumper)
        {
            _dumpPlayId = kind == LooseKind.Dump ? _play?.Id : null;
            _play = null;
            _prepaid = false;
            SetAttacker(attackerSide);
            _battleAttacker = attackerSide;
            _looseKind = kind;
            _dumper = dumper;
            _battleRetry = false;
            _mode = Mode.LooseBattle;
        }

        /// <summary>
        /// <c>dump</c>: the puck flies to the target node (no check), the dumping team may change on the fly ("dump and
        /// change", O-2), then the loose-puck battle there, inside the same sequence (O-1).
        /// </summary>
        private void Dump(TeamSide team, Position shooter, GridPoint target)
        {
            Spend("dumpIn");
            ClearActionHistory();
            _lastAction[(int)team] = LastPuckAction.ShotOrDump;
            Skater dumper = _state.SkaterAt(team, shooter);
            _state.SetLoosePuck(TeamFrame.ToRink(target, team, _rink));
            StartLooseBattle(team, LooseKind.Dump, dumper);
            TryChangeOnTheFly(team);
            _dumpPrepaid = true;
        }

        private void LooseBattleStep()
        {
            TeamSide attacker = _battleAttacker;
            TeamSide defender = TeamSides.Opponent(attacker);
            if (_dumpPrepaid)
            {
                _dumpPrepaid = false;
            }
            else if (!Begin(_battleRetry ? Seconds("systemStep") + Seconds("loosePuck") : Seconds("loosePuck")))
            {
                return;
            }

            if (_battleRetry)
            {
                // M-6.5: both teams take one system step around the loose puck (Q-035 default: each by its own system).
                Spend("systemStep");
                _state.ApplySystem(attacker, Team(attacker).Plan.System, _tuning.Plays.MaxNodesPerBeat);
                ApplyDefence(defender, _tuning.Plays.MaxNodesPerBeat);
            }

            GridPoint spot = _state.PuckNode;
            Chase(attacker, spot);
            Chase(defender, spot);
            Position attackerPosition = NearestSkater(attacker, spot);
            Position defenderPosition = NearestSkater(defender, spot);
            Skater a = _state.SkaterAt(attacker, attackerPosition);
            Skater d = _state.SkaterAt(defender, defenderPosition);
            int attackerDistance = GridPoint.Chebyshev(_state.NodeOf(attacker, attackerPosition), spot);
            int defenderDistance = GridPoint.Chebyshev(_state.NodeOf(defender, defenderPosition), spot);

            CheckDefinition check = _tuning.GetCheck("loosePuck");
            double m = check.Modifiers.Get("distancePerNode") * (defenderDistance - attackerDistance)
                + ExtraPlayer(check, attacker, defender, spot)
                + OffSide(new[] { a }, attacker, new[] { d }, defender)
                + EnergyModifier(new[] { a.Id }, new[] { d.Id });
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

            PayCost(a, d);
            Spend("loosePuck");
            ClearActionHistory();
            if (outcome != BattleOutcome.NoWinner)
            {
                TeamSide winner = outcome == BattleOutcome.Win ? attacker : defender;
                Position winnerPosition = outcome == BattleOutcome.Win ? attackerPosition : defenderPosition;
                MoveSkaters(winner, new[] { new KeyValuePair<Position, GridPoint>(winnerPosition, TeamFrame.ToTeamView(spot, winner, _rink)) });
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

            if (outcome == BattleOutcome.Loss)
            {
                DropOrganization(attacker, spot);
                GainPossession(defender, defenderPosition, GainKind.LooseWin, true);
                return;
            }

            if (IsOffensiveSlot(attacker, _state.NodeOf(attacker, attackerPosition)))
            {
                NotePossession(attacker, attackerPosition, false);
                SecondChance(attacker, attackerPosition);
                return;
            }

            GainPossession(attacker, attackerPosition, GainKind.LooseWin, false);
        }

        /// <summary>O-10 <c>looseChasers</c> 2: the team's second-nearest skater moves toward the loose puck and is committed. No time.</summary>
        private void Chase(TeamSide team, GridPoint spot)
        {
            if (Team(team).Plan.Instructions.LooseChasers < 2)
            {
                return;
            }

            Position nearest = NearestSkater(team, spot);
            var others = new SortedDictionary<Position, GridPoint>();
            foreach (Position position in PositionOrder.All)
            {
                if (position != nearest)
                {
                    others.Add(position, _state.NodeOf(team, position));
                }
            }

            LineGeometry.Closest(others, new[] { spot }, spot, int.MaxValue, out Position chaser, out int unused);
            MoveSkaters(team, new[] { new KeyValuePair<Position, GridPoint>(chaser, TeamFrame.ToTeamView(spot, team, _rink)) }, _tuning.Plays.MaxNodesPerBeat);
            _committed[(int)team, (int)chaser] = true;
        }

        /// <summary>D-059, D-065, O-8: the attacking side's win in its offensive slot is shot at once, without play setup.</summary>
        private void SecondChance(TeamSide team, Position shooter)
        {
            SetAttacker(team);
            _play = null;
            if (!Begin(Seconds("shoot") + Seconds("rebound")))
            {
                return;
            }

            _secondChance = true;
            Shoot(team, shooter);
        }

        /// <summary>The node (home view) is a slot node of <paramref name="team"/>'s offensive zone, in its own view.</summary>
        private bool IsOffensiveSlot(TeamSide team, GridPoint node)
        {
            GridPoint view = TeamFrame.ToTeamView(node, team, _rink);
            return _rink.GetNode(view.X, view.Y).IsSlot && _rink.ZoneAtX(view.X) == RinkZone.Offensive;
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

            Goalie goalie = GoalieOf(TeamSides.Opponent(dumpingTeam));
            return -dumpIn.GetParameter("goaliePuckHandlingPerPoint") * (goalie.Stats[GoalieStat.PuckHandling] - _tuning.CheckFormula.ReferenceValue);
        }
    }
}
