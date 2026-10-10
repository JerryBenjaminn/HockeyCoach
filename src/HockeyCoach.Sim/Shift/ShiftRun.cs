using System;
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
    /// One shift in progress: a small state machine stepped until a stoppage or the cap. Core flow, faceoff, possession
    /// and play mode live here; shots and loose pucks are in the other parts of this class.
    /// State modifiers (organization, team pressure, energy) are constant until milestone 3 (D-046) and so contribute 0.
    /// </summary>
    internal sealed partial class ShiftRun
    {
        private readonly ShiftSetup _setup;
        private readonly Rink _rink;
        private readonly TuningConfig _tuning;
        private readonly IRandom _random;
        private readonly EventLog _log = new EventLog();
        private readonly ShiftState _state;
        private readonly bool[,] _droveNet = new bool[2, 5];
        private readonly double[] _takeawayTime = { double.NaN, double.NaN };
        private readonly GridPoint[] _takeawayNode = new GridPoint[2];
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

        // Action history for Royal Road (M-3) and crease xG (D-047, D-059): only the last puck action counts.
        private LastPass _lastPass;
        private bool _secondChance;

        internal ShiftRun(ShiftSetup setup, IRandom random)
        {
            _setup = setup;
            _rink = setup.Rink;
            _tuning = setup.Tuning;
            _random = random;
            _time = setup.StartTime;
            FaceoffSpot spot = FindSpot(setup.FaceoffSpotId);
            _state = new ShiftState(
                _rink,
                setup.Home.Skaters,
                setup.Away.Skaters,
                setup.Home.Goalie.Id,
                setup.Away.Goalie.Id,
                FaceoffStart(TeamSide.Home, spot),
                FaceoffStart(TeamSide.Away, spot),
                spot.Point);
        }

        private enum Mode
        {
            Faceoff,
            PlayPass,
            PlayBeat,
            SystemPossession,
            LooseBattle,
        }

        internal ShiftResult Run()
        {
            while (!_ended)
            {
                if (_steps >= _setup.MaxSteps || _log.Count >= _setup.MaxSteps)
                {
                    _endReason = ShiftEndReason.StepCap;
                    break;
                }

                _steps++;
                Step();
            }

            return new ShiftResult(_log, _endReason, _goals[(int)TeamSide.Home], _goals[(int)TeamSide.Away], _time, _steps);
        }

        private void Step()
        {
            switch (_mode)
            {
                case Mode.Faceoff:
                    Faceoff();
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
                default:
                    LooseBattleStep();
                    break;
            }
        }

        // ---------------------------------------------------------------- faceoff

        private IReadOnlyDictionary<Position, GridPoint> FaceoffStart(TeamSide team, FaceoffSpot spot)
        {
            GridPoint teamSpot = TeamFrame.ToTeamView(spot.Point, team, _rink);
            string teamSpotId = SpotIdAt(teamSpot);
            Play play = teamSpotId == null ? null : PlaySelector.FaceoffPlay(Team(team).Plan, teamSpotId, out bool mirrored);
            if (play == null)
            {
                return FaceoffFormation.Default(teamSpot, _rink);
            }

            Play used = PlayMirror.ShouldMirrorAtSpot(play, teamSpotId) ? PlayMirror.Mirror(play, _rink) : play;
            _faceoffPlays[(int)team] = used;
            return used.StartPositions;
        }

        private void Faceoff()
        {
            CheckDefinition check = _tuning.GetCheck("faceoff");
            Skater home = _state.SkaterAt(TeamSide.Home, Position.Center);
            Skater away = _state.SkaterAt(TeamSide.Away, Position.Center);
            double m = check.Modifiers.Get("homeAdvantage") + OffSide(new[] { home }, TeamSide.Home, new[] { away }, TeamSide.Away);
            CheckResult result = SidedCheck.Resolve(
                _tuning.CheckFormula,
                check,
                new CheckParticipants().With("centre", home),
                new CheckParticipants().With("centre", away),
                m,
                _random);
            TeamSide winner = result.Success ? TeamSide.Home : TeamSide.Away;
            GridPoint spot = _state.PuckNode;
            _time += _tuning.Time.GetSecondsPerAction("faceoff");
            Play faceoffPlay = _faceoffPlays[(int)winner];
            Position receiver = faceoffPlay != null ? faceoffPlay.PuckCarrier : DotSideDefence(winner, spot);
            _state.GivePuckTo(winner, receiver);
            _log.Append(new FaceoffEvent(Context(null, null), home.Id, away.Id, winner, LocationOf(spot)));

            if (faceoffPlay != null)
            {
                SetAttacker(winner);
                BeginPlay(faceoffPlay, false);
            }
            else
            {
                GainPossession(winner, receiver);
            }
        }

        /// <summary>
        /// D-060 default receiver without a faceoff play: the defenceman on the dot's side in the winner's view (left spot
        /// and the middle lane → LD, right → RD).
        /// </summary>
        private Position DotSideDefence(TeamSide winner, GridPoint spot)
        {
            GridPoint view = TeamFrame.ToTeamView(spot, winner, _rink);
            return _rink.LaneOf(view.Y) == Lane.Right ? Position.RightDefence : Position.LeftDefence;
        }

        private FaceoffLocation LocationOf(GridPoint homeView)
        {
            switch (_rink.ZoneAtX(homeView.X))
            {
                case RinkZone.Defensive: return FaceoffLocation.HomeZone;
                case RinkZone.Offensive: return FaceoffLocation.AwayZone;
                default: return FaceoffLocation.NeutralZone;
            }
        }

        // ---------------------------------------------------------------- possession and play selection

        /// <summary>D-036: the team gains the puck with <paramref name="holder"/>; choose a play or keep it in system mode.</summary>
        private void GainPossession(TeamSide team, Position holder)
        {
            SetAttacker(team);
            _play = null;
            TrySelectPlay(team, holder);
        }

        private void TrySelectPlay(TeamSide team, Position holder)
        {
            RinkZone zone = _rink.ZoneAtX(TeamView(team, team, holder).X);
            Play play = PlaySelector.Select(Team(team).Plan, zone, holder, out bool needsPass);
            if (play == null)
            {
                _mode = Mode.SystemPossession;
                return;
            }

            GridPoint puck = TeamFrame.ToTeamView(_state.PuckNode, team, _rink);
            Play used = PlayMirror.ShouldMirror(play, puck, _rink) ? PlayMirror.Mirror(play, _rink) : play;
            Setup(team, used);
            BeginPlay(used, needsPass);
        }

        /// <summary>D-038: everyone moves to the play's start nodes and the defence to its system targets; costs setup time.</summary>
        private void Setup(TeamSide team, Play play)
        {
            _time += _tuning.Time.SetupSeconds;
            MoveSkaters(team, play.StartPositions);
            TeamSide defending = TeamSides.Opponent(team);
            _state.ApplySystem(defending, Team(defending).Plan.System, Unbounded);
            ClearActionHistory();
        }

        private void BeginPlay(Play play, bool needsPass)
        {
            _play = play;
            _beat = 0;
            _mode = needsPass ? Mode.PlayPass : Mode.PlayBeat;
        }

        /// <summary>D-036: a checked pass from the holder to the play's puck carrier starts the play.</summary>
        private void InitialPass()
        {
            TeamSide team = _attacker;
            Position holder = _state.CarrierPosition.Value;
            if (holder == _play.PuckCarrier || Pass(team, holder, _play.PuckCarrier))
            {
                _mode = Mode.PlayBeat;
            }
        }

        private void RunBeat()
        {
            TeamSide team = _attacker;
            Beat beat = _play.Beats[_beat];
            MoveSkaters(team, beat.Moves);

            TeamSide defending = TeamSides.Opponent(team);
            _state.ApplySystem(defending, Team(defending).Plan.System, _tuning.Plays.MaxNodesPerBeat);

            bool continues;
            PlayAction action = beat.Action;
            switch (action.Type)
            {
                case PlayActionType.Skate:
                    continues = Skate(team, action.Actor, action.Target.Value);
                    break;
                case PlayActionType.Pass:
                    continues = Pass(team, action.Actor, action.Receiver.Value);
                    break;
                case PlayActionType.DriveNet:
                    DriveNet(team, action.Actor);
                    continues = true;
                    break;
                case PlayActionType.Shoot:
                    Shoot(team, action.Actor);
                    continues = false;
                    break;
                default:
                    Dump(team, action.Actor, action.Target.Value);
                    continues = false;
                    break;
            }

            if (!continues || _mode != Mode.PlayBeat)
            {
                return;
            }

            _beat++;
            if (_beat >= _play.Beats.Count)
            {
                _play = null;
                _mode = Mode.SystemPossession;
            }
        }

        /// <summary>System mode with the puck: one system step (the defence moves), then a new play choice (Q-033 default).</summary>
        private void SystemPossessionStep()
        {
            TeamSide team = _attacker;
            _time += _tuning.Time.GetSecondsPerAction("systemStep");
            TeamSide defending = TeamSides.Opponent(team);
            _state.ApplySystem(defending, Team(defending).Plan.System, _tuning.Plays.MaxNodesPerBeat);
            ClearActionHistory();
            TrySelectPlay(team, _state.CarrierPosition.Value);
        }

        // ---------------------------------------------------------------- puck actions

        /// <summary>
        /// <c>skate</c> (D-032, D-039): zoneEntryCarry into the offensive zone, breakout out of the defensive zone,
        /// otherwise deke against the route defender (M-1). Failure: the route defender takes the puck (D-040).
        /// </summary>
        private bool Skate(TeamSide team, Position carrier, GridPoint target)
        {
            TeamSide defending = TeamSides.Opponent(team);
            GridPoint from = TeamView(team, team, carrier);
            RinkZone zoneFrom = _rink.ZoneAtX(from.X);
            RinkZone zoneTo = _rink.ZoneAtX(target.X);
            IReadOnlyDictionary<Position, GridPoint> defenders = NodesInView(defending, team);
            IReadOnlyList<GridPoint> route = LineGeometry.Nodes(from, target);
            LineGeometry.LineDefender(defenders, route, target, out Position routeDefender, out int distance);

            Skater carrierSkater = _state.SkaterAt(team, carrier);
            Skater defenderSkater = _state.SkaterAt(defending, routeDefender);
            bool entry = zoneTo == RinkZone.Offensive && zoneFrom != RinkZone.Offensive;
            bool breakout = !entry && zoneFrom == RinkZone.Defensive && zoneTo != RinkZone.Defensive;
            var participants = new CheckParticipants().With("carrier", carrierSkater);
            CheckDefinition check;
            double m;
            EntryNumbers numbers = default;
            if (entry)
            {
                check = _tuning.GetCheck("zoneEntryCarry");
                participants.With("nearestDefender", defenderSkater);
                numbers = Numbers(team, from);
                m = OffSide(new[] { carrierSkater }, team, new[] { defenderSkater }, defending);
            }
            else if (breakout)
            {
                check = _tuning.GetCheck("breakout");
                Skater[] forecheckers = Forecheckers(team);
                participants.With("forecheckers", forecheckers);
                m = OffSide(new[] { carrierSkater }, team, forecheckers, defending);
            }
            else
            {
                check = _tuning.GetCheck("deke");
                participants.With("nearestDefender", defenderSkater);
                m = check.Modifiers.GetTable("defenderDistance", distance)
                    + OffSide(new[] { carrierSkater }, team, new[] { defenderSkater }, defending);
            }

            CheckResult result = CheckResolver.Resolve(_tuning.CheckFormula, check, participants, m, _random);
            _time += _tuning.Time.GetSecondsPerAction("skate");
            ClearActionHistory();
            if (result.Success)
            {
                MoveSkaters(team, new[] { new KeyValuePair<Position, GridPoint>(carrier, target) });
                if (entry)
                {
                    _log.Append(new ControlledZoneEntryEvent(Context(), carrierSkater.Id, numbers, ZoneEntryMethod.Carry, ZoneEntryOutcome.Kept));
                }

                return true;
            }

            _state.GivePuckTo(defending, routeDefender);
            if (entry)
            {
                _log.Append(new ControlledZoneEntryEvent(Context(), carrierSkater.Id, numbers, ZoneEntryMethod.Carry, ZoneEntryOutcome.Lost));
            }

            Takeaway(team, carrier, defending, routeDefender);
            return false;
        }

        /// <summary>
        /// <c>pass</c>: a pass check against the pass lane defender (M-1 without the passer's node per D-062, M-2, M-7), or the breakout check when the pass
        /// leaves the defensive zone (D-039). Failure: interception or a loose puck at the lane defender (D-040).
        /// </summary>
        private bool Pass(TeamSide team, Position from, Position to)
        {
            TeamSide defending = TeamSides.Opponent(team);
            GridPoint passerNode = TeamView(team, team, from);
            GridPoint receiverNode = TeamView(team, team, to);
            IReadOnlyDictionary<Position, GridPoint> defenders = NodesInView(defending, team);
            IReadOnlyList<GridPoint> lane = LineGeometry.PassLane(passerNode, receiverNode);
            LineGeometry.LineDefender(defenders, lane, receiverNode, out Position laneDefender, out int distance);
            bool underPressure = UnderPressure(passerNode, defenders);
            bool crossIce = LineGeometry.IsCrossIce(passerNode, receiverNode, _rink);
            RinkZone passerZone = _rink.ZoneAtX(passerNode.X);
            RinkZone receiverZone = _rink.ZoneAtX(receiverNode.X);
            bool entry = passerZone != RinkZone.Offensive && receiverZone == RinkZone.Offensive;
            EntryNumbers numbers = entry ? Numbers(team, passerNode) : default;

            Skater passer = _state.SkaterAt(team, from);
            Skater receiver = _state.SkaterAt(team, to);
            Skater laneSkater = _state.SkaterAt(defending, laneDefender);
            CheckResult result;
            if (passerZone == RinkZone.Defensive && receiverZone != RinkZone.Defensive)
            {
                CheckDefinition breakout = _tuning.GetCheck("breakout");
                Skater[] forecheckers = Forecheckers(team);
                result = CheckResolver.Resolve(
                    _tuning.CheckFormula,
                    breakout,
                    new CheckParticipants().With("carrier", passer).With("forecheckers", forecheckers),
                    OffSide(new[] { passer }, team, forecheckers, defending),
                    _random);
            }
            else
            {
                CheckDefinition pass = _tuning.GetCheck("pass");
                double m = pass.Modifiers.GetTable("laneDefenderDistance", distance)
                    + (crossIce ? pass.Modifiers.Get("crossIce") : 0.0)
                    + (underPressure ? pass.Modifiers.Get("underPressure") : 0.0)
                    + OffSide(new[] { passer, receiver }, team, new[] { laneSkater }, defending);
                result = CheckResolver.Resolve(
                    _tuning.CheckFormula,
                    pass,
                    new CheckParticipants().With("passer", passer).With("receiver", receiver).With("nearestDefender", laneSkater),
                    m,
                    _random);
            }

            _time += _tuning.Time.GetSecondsPerAction("pass");
            ClearActionHistory();
            if (result.Success)
            {
                _state.GivePuckTo(team, to);
                _lastPass = new LastPass(team, from, to, passerNode, receiverNode, crossIce);
                _log.Append(new PassEvent(Context(), passer.Id, receiver.Id, underPressure, true));
                if (entry)
                {
                    _log.Append(new ControlledZoneEntryEvent(Context(), receiver.Id, numbers, ZoneEntryMethod.Pass, ZoneEntryOutcome.Kept));
                }

                return true;
            }

            bool interception = _random.Chance(_tuning.GetCheck("pass").GetParameter("interceptionShare"));
            if (interception)
            {
                _state.GivePuckTo(defending, laneDefender);
            }
            else
            {
                _state.SetLoosePuck(TeamFrame.ToRink(LooseSpot(_tuning.LoosePuckSpots.FailedPass, passerNode, defenders[laneDefender]), team, _rink));
            }

            _log.Append(new PassEvent(Context(), passer.Id, receiver.Id, underPressure, false));
            if (entry)
            {
                _log.Append(new ControlledZoneEntryEvent(Context(), receiver.Id, numbers, ZoneEntryMethod.Pass, ZoneEntryOutcome.Lost));
            }

            if (interception)
            {
                Takeaway(team, from, defending, laneDefender);
            }
            else
            {
                StartLooseBattle(team, LooseKind.Normal, null);
            }

            return false;
        }

        /// <summary><c>driveNet</c> (D-020, D-033): to the net front, no check, no event.</summary>
        private void DriveNet(TeamSide team, Position player)
        {
            MoveSkaters(team, new[] { new KeyValuePair<Position, GridPoint>(player, _rink.NetFrontOf(_rink.OpponentGoal)) });
            _droveNet[(int)team, (int)player] = _state.NodeOf(team, player).Equals(TeamFrame.ToRink(_rink.NetFrontOf(_rink.OpponentGoal), team, _rink));
            _time += _tuning.Time.GetSecondsPerAction("driveNet");
            ClearActionHistory();
        }

        /// <summary>A takeaway: log it, remember it for the turnover chance type, and hand possession over.</summary>
        private void Takeaway(TeamSide loserTeam, Position loser, TeamSide takerTeam, Position taker)
        {
            GridPoint node = _state.NodeOf(takerTeam, taker);
            _play = null;
            _log.Append(new TurnoverEvent(Context(), _state.PlayerId(loserTeam, loser), _state.PlayerId(takerTeam, taker), _rink.IdOf(node.X, node.Y)));
            _takeawayTime[(int)takerTeam] = _time;
            _takeawayNode[(int)takerTeam] = node;
            GainPossession(takerTeam, taker);
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>No step limit: a placement that reaches its target whatever the distance.</summary>
        private int Unbounded
        {
            get { return _rink.Length * _rink.Width; }
        }

        /// <summary>
        /// Moves skaters of the team to targets in its own view (D-058 order: carrier first, then C, LW, RW, LD, RD). Anyone who
        /// moves is no longer the one who drove the net (D-047).
        /// </summary>
        private void MoveSkaters(TeamSide team, IEnumerable<KeyValuePair<Position, GridPoint>> targets)
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

            _state.Move(team, ordered, Unbounded);
        }

        private TeamShiftSetup Team(TeamSide side)
        {
            return side == TeamSide.Home ? _setup.Home : _setup.Away;
        }

        private void SetAttacker(TeamSide team)
        {
            _hasAttacker = true;
            _attacker = team;
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

        private FaceoffSpot FindSpot(string id)
        {
            foreach (FaceoffSpot spot in _rink.FaceoffSpots)
            {
                if (string.Equals(spot.Id, id, StringComparison.Ordinal))
                {
                    return spot;
                }
            }

            throw new ArgumentException("Unknown faceoff spot " + id + ".");
        }

        private string SpotIdAt(GridPoint point)
        {
            foreach (FaceoffSpot spot in _rink.FaceoffSpots)
            {
                if (spot.Point.Equals(point))
                {
                    return spot.Id;
                }
            }

            return null;
        }

        private void End(ShiftEndReason reason, StoppageReason stoppage)
        {
            _log.Append(new StoppageEvent(Context(), stoppage));
            _endReason = reason;
            _ended = true;
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
