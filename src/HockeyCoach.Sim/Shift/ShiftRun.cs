using System.Collections.Generic;
using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.State;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// One shift segment: from a faceoff to the next stoppage or the end of the period (O-1, O-13), stepped as a small
    /// state machine. The core loop and shared helpers live here; the clock, faceoff, possession and play mode,
    /// transitions, puck actions, shots, loose pucks, line changes and state modifiers are in the other parts of this
    /// class. Persistent state (energy, organization, pressure, familiarity, lineups) lives in <see cref="GameState"/>.
    /// </summary>
    internal sealed partial class ShiftRun
    {
        private readonly ShiftSetup _setup;
        private readonly Rink _rink;
        private readonly TuningConfig _tuning;
        private readonly IRandom _random;
        private readonly EventLog _log;
        private readonly GameState _game;
        private readonly IShiftHost _host;
        private readonly ShiftState _state;
        private readonly bool[,] _droveNet = new bool[2, 5];
        private readonly bool[,] _committed = new bool[2, 5];
        private readonly bool[] _pinchActive = new bool[2];
        private readonly int[] _goals = new int[2];
        private readonly Play[] _faceoffPlays = new Play[2];

        private Mode _mode = Mode.Faceoff;
        private double _time;
        private int _steps;
        private bool _ended;
        private ShiftEndReason _endReason = ShiftEndReason.StepCap;

        // Possession and play.
        private bool _hasAttacker;
        private TeamSide _attacker;
        private Play _play;
        private int _beat;
        private double _familiarityPenalty;
        private bool _prepaid;
        private int _setupSteps;
        private int _rushActions;

        // Action history for Royal Road (M-3) and crease xG (D-047, D-059): only the last puck action counts.
        private LastPass _lastPass;
        private bool _secondChance;

        // Last shot, for the next faceoff spot (O-3).
        private bool _hasLastShot;
        private TeamSide _lastShotTeam;
        private GridPoint _lastShotNode;

        internal ShiftRun(ShiftSetup setup, IRandom random)
        {
            _setup = setup;
            _rink = setup.Rink;
            _tuning = setup.Tuning;
            _random = random;
            _time = setup.StartTime;
            ShiftContext context = setup.Context ?? StandaloneContext(setup);
            _game = context.State;
            _log = context.Log;
            _host = context.Host;
            _setupSteps = SetupSteps(_tuning.Time.SetupSeconds);
            ApplyStartUnits(context);
            FaceoffSpot spot = FindSpot(setup.FaceoffSpotId);
            _state = new ShiftState(
                _rink,
                _game.Lineup(TeamSide.Home).OnIce,
                _game.Lineup(TeamSide.Away).OnIce,
                _game.Lineup(TeamSide.Home).Goalie.Id,
                _game.Lineup(TeamSide.Away).Goalie.Id,
                FaceoffStart(TeamSide.Home, spot),
                FaceoffStart(TeamSide.Away, spot),
                spot.Point);
        }

        private enum Mode
        {
            Faceoff,
            SelectPlay,
            PlayPass,
            PlayBeat,
            SystemPossession,
            LooseBattle,
            Rush,
            Regroup,
        }

        private enum LastPuckAction
        {
            Other,
            ShotOrDump,
        }

        internal ShiftResult Run()
        {
            LogStartChanges();
            while (!_ended)
            {
                if (_steps >= _setup.MaxSteps)
                {
                    _endReason = ShiftEndReason.StepCap;
                    break;
                }

                _steps++;
                TrackEntry();
                Step();
            }

            return new ShiftResult(
                _log, _endReason, _goals[(int)TeamSide.Home], _goals[(int)TeamSide.Away], _time, _steps, _hasLastShot, _lastShotTeam, _lastShotNode);
        }

        private void Step()
        {
            switch (_mode)
            {
                case Mode.Faceoff:
                    Faceoff();
                    break;
                case Mode.SelectPlay:
                    SelectPlayStep();
                    break;
                case Mode.PlayPass:
                    InitialPass();
                    break;
                case Mode.PlayBeat:
                    RunBeat();
                    break;
                case Mode.SystemPossession:
                    SystemPossessionStep();
                    break;
                case Mode.Rush:
                    RushStep();
                    break;
                case Mode.Regroup:
                    RegroupStep();
                    break;
                default:
                    LooseBattleStep();
                    break;
            }
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>No step limit: a placement that reaches its target whatever the distance.</summary>
        private int Unbounded
        {
            get { return _rink.Length * _rink.Width; }
        }

        /// <summary>O-6: defenders' steps in a setup or regroup of <paramref name="seconds"/>.</summary>
        private int SetupSteps(double seconds)
        {
            double skate = _tuning.Time.GetSecondsPerAction("skate");
            return skate > 0.0 ? (int)System.Math.Floor((seconds * _tuning.Plays.MaxNodesPerBeat / skate) + 1e-9) : Unbounded;
        }

        /// <summary>
        /// Moves skaters of the team toward targets in its own view, at most <paramref name="maxSteps"/> each (D-058 order:
        /// carrier first, then C, LW, RW, LD, RD). Anyone who moves is no longer the one who drove the net (D-047).
        /// </summary>
        private void MoveSkaters(TeamSide team, IEnumerable<KeyValuePair<Position, GridPoint>> targets, int maxSteps)
        {
            var byPosition = new SortedDictionary<Position, GridPoint>();
            foreach (KeyValuePair<Position, GridPoint> target in targets)
            {
                byPosition.Add(target.Key, target.Value);
            }

            Position? carrier = _state.HasCarrier && _state.CarrierTeam == team ? _state.CarrierPosition : null;
            var ordered = new List<KeyValuePair<Position, GridPoint>>();
            foreach (Position position in OccupancyResolver.AttackOrder(carrier, byPosition.Keys))
            {
                ordered.Add(new KeyValuePair<Position, GridPoint>(position, byPosition[position]));
                _droveNet[(int)team, (int)position] = false;
            }

            _state.Move(team, ordered, maxSteps);
        }

        private void MoveSkaters(TeamSide team, IEnumerable<KeyValuePair<Position, GridPoint>> targets)
        {
            MoveSkaters(team, targets, Unbounded);
        }

        /// <summary>The team's system movement (D-041) with its pinch instruction (O-10).</summary>
        private void ApplyDefence(TeamSide defending, int maxSteps)
        {
            _state.ApplySystem(defending, Team(defending).Plan.System, maxSteps, Team(defending).Plan.Instructions.Pinch, out bool pinched);
            _pinchActive[(int)defending] = pinched;
        }

        private TeamShiftSetup Team(TeamSide side)
        {
            return side == TeamSide.Home ? _setup.Home : _setup.Away;
        }

        private Goalie GoalieOf(TeamSide side)
        {
            return _game.Lineup(side).Goalie;
        }

        private void ClearActionHistory()
        {
            _lastPass = null;
            _secondChance = false;
        }

        /// <summary>A skater's node in <paramref name="viewer"/>'s own view.</summary>
        private GridPoint TeamView(TeamSide viewer, TeamSide owner, Position position)
        {
            return TeamFrame.ToTeamView(_state.NodeOf(owner, position), viewer, _rink);
        }

        /// <summary>All five skaters of <paramref name="owner"/> in <paramref name="viewer"/>'s own view.</summary>
        private IReadOnlyDictionary<Position, GridPoint> NodesInView(TeamSide owner, TeamSide viewer)
        {
            var nodes = new SortedDictionary<Position, GridPoint>();
            foreach (Position position in PositionOrder.All)
            {
                nodes.Add(position, TeamView(viewer, owner, position));
            }

            return nodes;
        }

        /// <summary>The zone of a home-view node in <paramref name="team"/>'s view.</summary>
        private RinkZone ZoneFor(TeamSide team, GridPoint rinkNode)
        {
            return _rink.ZoneAtX(TeamFrame.ToTeamView(rinkNode, team, _rink).X);
        }

        /// <summary>M-7: an opposing skater within <c>pressure.underPressureNodes</c>.</summary>
        private bool UnderPressure(GridPoint player, IReadOnlyDictionary<Position, GridPoint> opponents)
        {
            foreach (KeyValuePair<Position, GridPoint> opponent in opponents)
            {
                if (GridPoint.Chebyshev(player, opponent.Value) <= _tuning.Pressure.UnderPressureNodes)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>M-8: attackers at or past the centre line vs defenders at or beyond the puck's x (attacker's view).</summary>
        private EntryNumbers Numbers(TeamSide team, GridPoint puck)
        {
            int attackers = 0;
            int defenders = 0;
            Position carrier = _state.CarrierPosition.Value;
            foreach (Position position in PositionOrder.All)
            {
                if (position == carrier || TeamView(team, team, position).X >= _rink.CentreLineX)
                {
                    attackers++;
                }

                if (TeamView(team, TeamSides.Opponent(team), position).X >= puck.X)
                {
                    defenders++;
                }
            }

            return new EntryNumbers(attackers, defenders);
        }

        /// <summary>
        /// M-1 <c>forecheckers</c>: the defending team's forwards (roles F1–F3) in the attacking team's defensive zone;
        /// if none, F1.
        /// </summary>
        private Skater[] Forecheckers(TeamSide attacking)
        {
            TeamSide defending = TeamSides.Opponent(attacking);
            GridPoint puck = TeamFrame.ToTeamView(_state.PuckNode, defending, _rink);
            IReadOnlyDictionary<SystemRole, Position> roles = RoleAssigner.Assign(_state.NodesInTeamView(defending), puck, _rink);
            var forecheckers = new List<Skater>();
            foreach (SystemRole role in new[] { SystemRole.F1, SystemRole.F2, SystemRole.F3 })
            {
                Position position = roles[role];
                if (_rink.ZoneAtX(TeamView(attacking, defending, position).X) == RinkZone.Defensive)
                {
                    forecheckers.Add(_state.SkaterAt(defending, position));
                }
            }

            if (forecheckers.Count == 0)
            {
                forecheckers.Add(_state.SkaterAt(defending, roles[SystemRole.F1]));
            }

            return forecheckers.ToArray();
        }

        /// <summary>Off-side modifier (D-024, once per side, Q-020).</summary>
        private double OffSide(IEnumerable<Skater> attackerSide, TeamSide attackerTeam, IEnumerable<Skater> defenderSide, TeamSide defenderTeam)
        {
            return OffSideModifier.Contribution(
                _tuning.Positions.OffSideCheckModifier,
                AnyOffSide(attackerSide, attackerTeam),
                AnyOffSide(defenderSide, defenderTeam));
        }

        private bool AnyOffSide(IEnumerable<Skater> skaters, TeamSide team)
        {
            OnIceSkaters unit = _state.UnitOf(team);
            foreach (Skater skater in skaters)
            {
                if (unit.IsOffSide(skater))
                {
                    return true;
                }
            }

            return false;
        }

        private EventContext Context()
        {
            TeamSide defending = TeamSides.Opponent(_attacker);
            return Context(_play?.Id, _hasAttacker ? Team(defending).Plan.System.Id : null);
        }

        private EventContext Context(string playId, string systemId)
        {
            return new EventContext(_setup.Period, _time, _state.Strength, _state.Snapshot(), playId, systemId);
        }

        private void End(ShiftEndReason reason, StoppageReason stoppage)
        {
            _log.Append(new StoppageEvent(Context(), stoppage));
            _endReason = reason;
            _ended = true;
            _prepaid = false;
        }

        /// <summary>The last puck action if it was a completed pass (M-3 condition 1, D-047 condition 2).</summary>
        private sealed class LastPass
        {
            internal LastPass(TeamSide team, Position from, Position to, GridPoint passerNode, GridPoint receiverNode, bool crossIce)
            {
                Team = team;
                From = from;
                To = to;
                PasserNode = passerNode;
                ReceiverNode = receiverNode;
                CrossIce = crossIce;
            }

            internal TeamSide Team { get; }

            internal Position From { get; }

            internal Position To { get; }

            internal GridPoint PasserNode { get; }

            internal GridPoint ReceiverNode { get; }

            internal bool CrossIce { get; }
        }
    }
}
