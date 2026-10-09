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
    /// non-goal node. Goalies stand on their own goal node. Two skaters may share a node.
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
            OnIceSkaters unit = _units[(int)team];
            switch (position)
            {
                case Position.LeftWing: return unit.Forwards.LeftWing.Id;
                case Position.Center: return unit.Forwards.Center.Id;
                case Position.RightWing: return unit.Forwards.RightWing.Id;
                case Position.LeftDefence: return unit.Defence.LeftDefence.Id;
                default: return unit.Defence.RightDefence.Id;
            }
        }

        /// <summary>Moves a skater (home view). Throws for a node outside the grid or a goal node (D-033).</summary>
        public void Place(TeamSide team, Position position, GridPoint node)
        {
            RequireSkaterNode(node, nameof(node));
            _nodes[(int)team][position] = node;
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
        /// defending team's view and advances every defender up to <paramref name="maxSteps"/> steps toward its target.
        /// </summary>
        /// <returns>The evaluated targets (defending team's view).</returns>
        public SystemTargets ApplySystem(TeamSide defending, DefensiveSystem system, int maxSteps)
        {
            GridPoint puck = TeamFrame.ToTeamView(PuckNode, defending, _rink);
            IReadOnlyDictionary<Position, GridPoint> skaters = NodesInTeamView(defending);
            SystemTargets targets = SystemTargetResolver.Resolve(system, _rink, puck, PuckState, skaters);
            foreach (KeyValuePair<Position, GridPoint> target in targets.Targets)
            {
                GridPoint reached = SystemMovement.Advance(skaters[target.Key], target.Value, puck, maxSteps, _rink);
                Place(defending, target.Key, TeamFrame.ToRink(reached, defending, _rink));
            }

            return targets;
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
                _nodes[(int)team][position] = rinkNode;
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
