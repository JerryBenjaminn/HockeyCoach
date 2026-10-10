using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.State
{
    /// <summary>
    /// Positions of the ten skaters and the puck during a shift, stored in the fixed rink frame (home team's view).
    /// Invariants: every skater stands on a non-goal node (D-033); the puck has exactly one carrier or is loose on a
    /// non-goal node. Goalies stand on their own goal node. At most one skater of a team per node; opponents may share
    /// (D-058). Multi-skater moves go through <see cref="OccupancyResolver"/>.
    /// </summary>
    public sealed class ShiftState
    {
        private readonly Rink _rink;
        private readonly OnIceSkaters[] _units = new OnIceSkaters[2];
        private readonly int[] _goalieIds = new int[2];
        private readonly SortedDictionary<Position, GridPoint>[] _nodes =
        {
            new SortedDictionary<Position, GridPoint>(), new SortedDictionary<Position, GridPoint>(),
        };

        private TeamSide _carrierTeam;
        private Position? _carrier;
        private GridPoint _loosePuck;

        /// <summary>Creates the state with everyone placed and a loose puck.</summary>
        /// <param name="rink">The rink.</param>
        /// <param name="home">Home skaters on the ice.</param>
        /// <param name="away">Away skaters on the ice.</param>
        /// <param name="homeGoalieId">Home goalie's player id.</param>
        /// <param name="awayGoalieId">Away goalie's player id.</param>
        /// <param name="homeNodes">Home skater nodes, in the home team's view.</param>
        /// <param name="awayNodes">Away skater nodes, in the away team's own view (rotated in here).</param>
        /// <param name="loosePuck">Loose puck node, in the home team's view.</param>
        public ShiftState(
            Rink rink,
            OnIceSkaters home,
            OnIceSkaters away,
            int homeGoalieId,
            int awayGoalieId,
            IReadOnlyDictionary<Position, GridPoint> homeNodes,
            IReadOnlyDictionary<Position, GridPoint> awayNodes,
            GridPoint loosePuck)
        {
            _rink = rink ?? throw new ArgumentNullException(nameof(rink));
            _units[(int)TeamSide.Home] = home ?? throw new ArgumentNullException(nameof(home));
            _units[(int)TeamSide.Away] = away ?? throw new ArgumentNullException(nameof(away));
            _goalieIds[(int)TeamSide.Home] = homeGoalieId;
            _goalieIds[(int)TeamSide.Away] = awayGoalieId;
            PlaceAll(TeamSide.Home, homeNodes, nameof(homeNodes));
            PlaceAll(TeamSide.Away, awayNodes, nameof(awayNodes));
            SetLoosePuck(loosePuck);
        }

        /// <summary>The rink.</summary>
        public Rink Rink
        {
            get { return _rink; }
        }

        /// <summary>Whether someone carries the puck.</summary>
        public bool HasCarrier
        {
            get { return _carrier.HasValue; }
        }

        /// <summary>The carrying team; only meaningful when <see cref="HasCarrier"/>.</summary>
        public TeamSide CarrierTeam
        {
            get { return _carrierTeam; }
        }

        /// <summary>The carrying position, or null for a loose puck.</summary>
        public Position? CarrierPosition
        {
            get { return _carrier; }
        }

        /// <summary>Puck node in the home team's view (the carrier's node when carried).</summary>
        public GridPoint PuckNode
        {
            get { return _carrier.HasValue ? NodeOf(_carrierTeam, _carrier.Value) : _loosePuck; }
        }

        /// <summary>Puck state for system conditions.</summary>
        public PuckState PuckState
        {
            get { return _carrier.HasValue ? PuckState.Controlled : PuckState.Loose; }
        }

        /// <summary>A skater's node in the home team's view.</summary>
        public GridPoint NodeOf(TeamSide team, Position position)
        {
            return _nodes[(int)team][position];
        }

        /// <summary>The five skater nodes of <paramref name="team"/> in that team's own view, in position order.</summary>
        public IReadOnlyDictionary<Position, GridPoint> NodesInTeamView(TeamSide team)
        {
            var view = new SortedDictionary<Position, GridPoint>();
            foreach (KeyValuePair<Position, GridPoint> entry in _nodes[(int)team])
            {
                view.Add(entry.Key, TeamFrame.ToTeamView(entry.Value, team, _rink));
            }

            return view;
        }

        /// <summary>Player id of the skater playing <paramref name="position"/>.</summary>
        public int PlayerId(TeamSide team, Position position)
        {
            return SkaterAt(team, position).Id;
        }

        /// <summary>The skater playing <paramref name="position"/> for <paramref name="team"/>.</summary>
        public Skater SkaterAt(TeamSide team, Position position)
        {
            OnIceSkaters unit = _units[(int)team];
            switch (position)
            {
                case Position.LeftWing: return unit.Forwards.LeftWing;
                case Position.Center: return unit.Forwards.Center;
                case Position.RightWing: return unit.Forwards.RightWing;
                case Position.LeftDefence: return unit.Defence.LeftDefence;
                default: return unit.Defence.RightDefence;
            }
        }

        /// <summary>The on-ice unit of <paramref name="team"/>.</summary>
        public OnIceSkaters UnitOf(TeamSide team)
        {
            return _units[(int)team];
        }

        /// <summary>The goalie id of <paramref name="team"/>.</summary>
        public int GoalieId(TeamSide team)
        {
            return _goalieIds[(int)team];
        }

        /// <summary>
        /// Puts a skater on <paramref name="node"/> (home view) without resolution. Throws for a node outside the grid, a
        /// goal node (D-033) or a node a teammate already stands on (D-058).
        /// </summary>
        public void Place(TeamSide team, Position position, GridPoint node)
        {
            RequireSkaterNode(node, nameof(node));
            foreach (KeyValuePair<Position, GridPoint> entry in _nodes[(int)team])
            {
                if (entry.Key != position && entry.Value.Equals(node))
                {
                    throw new InvalidOperationException(
                        Positions.ToName(entry.Key) + " already stands on " + node + "; one skater per team per node (D-058).");
                }
            }

            _nodes[(int)team][position] = node;
        }

        /// <summary>
        /// Moves skaters of <paramref name="team"/> toward targets given in that team's own view, in the given order, each
        /// at most <paramref name="maxSteps"/> steps by the movement rules (D-053), with D-058 conflicts resolved by
        /// <see cref="OccupancyResolver"/>. Skaters not listed stay put.
        /// </summary>
        /// <param name="team">The moving team.</param>
        /// <param name="orderedTargets">Position → target node (team's own view), in processing order.</param>
        /// <param name="maxSteps">Most steps per mover.</param>
        public void Move(TeamSide team, IReadOnlyList<KeyValuePair<Position, GridPoint>> orderedTargets, int maxSteps)
        {
            if (orderedTargets == null)
            {
                throw new ArgumentNullException(nameof(orderedTargets));
            }

            GridPoint puck = TeamFrame.ToTeamView(PuckNode, team, _rink);
            IReadOnlyDictionary<Position, GridPoint> skaters = NodesInTeamView(team);
            var moves = new List<OccupancyMove>();
            foreach (KeyValuePair<Position, GridPoint> target in orderedTargets)
            {
                RequireSkaterNode(target.Value, nameof(orderedTargets));
                moves.Add(new OccupancyMove(target.Key, SystemMovement.Path(skaters[target.Key], target.Value, puck, maxSteps, _rink)));
            }

            SetTeam(team, OccupancyResolver.Resolve(skaters, moves, _rink));
        }

        /// <summary>Gives the puck to a skater.</summary>
        public void GivePuckTo(TeamSide team, Position position)
        {
            _carrierTeam = team;
            _carrier = position;
        }

        /// <summary>Makes the puck loose at <paramref name="node"/> (home view). A loose puck never rests on a goal node.</summary>
        public void SetLoosePuck(GridPoint node)
        {
            RequireSkaterNode(node, nameof(node));
            _carrier = null;
            _loosePuck = node;
        }

        /// <summary>
        /// Moves the defending team per its system (D-041: the defence moves every beat): evaluates the system in the
        /// defending team's view and advances every defender up to <paramref name="maxSteps"/> steps toward its target,
        /// in role order F1, D1, D2, F2, F3 with D-058 conflicts resolved by <see cref="OccupancyResolver"/>.
        /// </summary>
        /// <returns>The evaluated targets (defending team's view).</returns>
        public SystemTargets ApplySystem(TeamSide defending, DefensiveSystem system, int maxSteps)
        {
            return ApplySystem(defending, system, maxSteps, false, out bool unused);
        }

        /// <summary>
        /// Like <see cref="ApplySystem(TeamSide, DefensiveSystem, int)"/>, with the <c>pinch</c> instruction (O-10): when
        /// <paramref name="pinch"/> is set and the puck is on the boards (y 0 or width − 1) high in the defending team's
        /// offensive zone (offensive zone xMin ≤ x &lt; opponent goal x, defending team's view), D1's target is the puck node.
        /// </summary>
        /// <param name="defending">The moving team.</param>
        /// <param name="system">Its system.</param>
        /// <param name="maxSteps">Most steps per skater.</param>
        /// <param name="pinch">The team's pinch instruction.</param>
        /// <param name="pinched">Whether D1's target was replaced.</param>
        public SystemTargets ApplySystem(TeamSide defending, DefensiveSystem system, int maxSteps, bool pinch, out bool pinched)
        {
            GridPoint puck = TeamFrame.ToTeamView(PuckNode, defending, _rink);
            IReadOnlyDictionary<Position, GridPoint> skaters = NodesInTeamView(defending);
            SystemTargets targets = SystemTargetResolver.Resolve(system, _rink, puck, PuckState, skaters);
            pinched = pinch && IsPinchSpot(puck);
            var ordered = new List<KeyValuePair<Position, GridPoint>>();
            foreach (SystemRole role in OccupancyResolver.SystemOrder)
            {
                Position position = targets.Roles[role];
                GridPoint target = pinched && role == SystemRole.D1 ? puck : targets.Targets[position];
                ordered.Add(new KeyValuePair<Position, GridPoint>(position, target));
            }

            Move(defending, ordered, maxSteps);
            return targets;
        }

        /// <summary>O-10 pinch spot: boards, high in the offensive zone (team's own view).</summary>
        public bool IsPinchSpot(GridPoint teamViewPuck)
        {
            int offensiveMin = int.MaxValue;
            foreach (ZoneRange zone in _rink.Zones)
            {
                if (zone.Zone == RinkZone.Offensive)
                {
                    offensiveMin = zone.XMin;
                }
            }

            bool boards = teamViewPuck.Y == 0 || teamViewPuck.Y == _rink.Width - 1;
            return boards && teamViewPuck.X >= offensiveMin && teamViewPuck.X < _rink.OpponentGoal.X;
        }

        /// <summary>
        /// Puts another unit on the ice (a line change, O-2): the incoming skaters take the outgoing skaters' nodes at the
        /// same positions; the puck carrier's position keeps the puck.
        /// </summary>
        public void ReplaceUnit(TeamSide team, OnIceSkaters unit)
        {
            _units[(int)team] = unit ?? throw new ArgumentNullException(nameof(unit));
        }

        /// <summary>A copy of every skater node (home view), to undo a move with <see cref="RestoreNodes"/>.</summary>
        public IReadOnlyDictionary<Position, GridPoint>[] SaveNodes()
        {
            return new IReadOnlyDictionary<Position, GridPoint>[]
            {
                new SortedDictionary<Position, GridPoint>(_nodes[0]), new SortedDictionary<Position, GridPoint>(_nodes[1]),
            };
        }

        /// <summary>Restores nodes saved by <see cref="SaveNodes"/>.</summary>
        public void RestoreNodes(IReadOnlyDictionary<Position, GridPoint>[] saved)
        {
            if (saved == null || saved.Length != 2)
            {
                throw new ArgumentException("Expected the two teams' nodes.", nameof(saved));
            }

            for (int team = 0; team < 2; team++)
            {
                _nodes[team].Clear();
                foreach (KeyValuePair<Position, GridPoint> entry in saved[team])
                {
                    _nodes[team].Add(entry.Key, entry.Value);
                }
            }
        }

        /// <summary>Skaters on the ice per team.</summary>
        public Strength Strength
        {
            get { return new Strength(_nodes[0].Count, _nodes[1].Count); }
        }

        /// <summary>Snapshot of every player and the puck as node ids (home view) for an event.</summary>
        public PlacementSnapshot Snapshot()
        {
            var players = new List<PlayerPlacement>();
            foreach (TeamSide team in new[] { TeamSide.Home, TeamSide.Away })
            {
                foreach (KeyValuePair<Position, GridPoint> entry in _nodes[(int)team])
                {
                    players.Add(new PlayerPlacement(PlayerId(team, entry.Key), team, _rink.IdOf(entry.Value.X, entry.Value.Y), false));
                }

                GridPoint goal = team == TeamSide.Home ? _rink.OwnGoal : _rink.OpponentGoal;
                players.Add(new PlayerPlacement(_goalieIds[(int)team], team, _rink.IdOf(goal.X, goal.Y), true));
            }

            GridPoint puck = PuckNode;
            int? carrierId = _carrier.HasValue ? PlayerId(_carrierTeam, _carrier.Value) : (int?)null;
            return new PlacementSnapshot(players, carrierId, _rink.IdOf(puck.X, puck.Y));
        }

        private void PlaceAll(TeamSide team, IReadOnlyDictionary<Position, GridPoint> nodes, string paramName)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(paramName);
            }

            foreach (Position position in new[] { Position.Center, Position.LeftWing, Position.RightWing, Position.LeftDefence, Position.RightDefence })
            {
                if (!nodes.TryGetValue(position, out GridPoint node))
                {
                    throw new ArgumentException("No start node for " + Positions.ToName(position) + ".", paramName);
                }

                GridPoint rinkNode = TeamFrame.ToRink(node, team, _rink);
                RequireSkaterNode(rinkNode, paramName);
                if (_nodes[(int)team].ContainsValue(rinkNode))
                {
                    throw new ArgumentException("Two skaters start on " + node + "; one skater per team per node (D-058).", paramName);
                }

                _nodes[(int)team][position] = rinkNode;
            }
        }

        /// <summary>Sets all of a team's nodes at once (team's own view in, stored in the home view).</summary>
        private void SetTeam(TeamSide team, IReadOnlyDictionary<Position, GridPoint> teamView)
        {
            var seen = new HashSet<GridPoint>();
            foreach (KeyValuePair<Position, GridPoint> entry in teamView)
            {
                GridPoint rinkNode = TeamFrame.ToRink(entry.Value, team, _rink);
                RequireSkaterNode(rinkNode, nameof(teamView));
                if (!seen.Add(rinkNode))
                {
                    throw new InvalidOperationException("Two skaters on " + entry.Value + " after a move (D-058).");
                }
            }

            foreach (KeyValuePair<Position, GridPoint> entry in teamView)
            {
                _nodes[(int)team][entry.Key] = TeamFrame.ToRink(entry.Value, team, _rink);
            }
        }

        private void RequireSkaterNode(GridPoint node, string paramName)
        {
            if (!_rink.Contains(node))
            {
                throw new ArgumentOutOfRangeException(paramName, node + " is outside the rink.");
            }

            if (_rink.IsGoalNode(node))
            {
                throw new ArgumentException(node + " is a goal node; no skater or loose puck rests there (D-033).", paramName);
            }
        }
    }
}
