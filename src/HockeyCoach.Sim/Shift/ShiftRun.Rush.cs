using System.Collections.Generic;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// Transitions (D-007, data-schema.md O-7): the built-in rush without a play or setup, and the regroup. Each rush
    /// action runs like a beat: support moves → defence system movement → the carrier's action.
    /// </summary>
    internal sealed partial class ShiftRun
    {
        private enum RushAction
        {
            Shoot,
            Pass,
            Skate,
        }

        private void RushStep()
        {
            TeamSide team = _attacker;
            TeamSide defending = TeamSides.Opponent(team);
            if (_game.Organization(defending) >= _tuning.Organization.OrganizedThreshold - TimeEpsilon
                || _rushActions >= _tuning.Transitions.RushMaxActions)
            {
                SelectNext(_tuning.Time.SetupSeconds);
                return;
            }

            IReadOnlyDictionary<Position, GridPoint>[] saved = _state.SaveNodes();
            bool[] pinch = (bool[])_pinchActive.Clone();
            MoveRushSupport(team);
            ApplyDefence(defending, _tuning.Plays.MaxNodesPerBeat);
            Position carrier = _state.CarrierPosition.Value;
            RushAction action = ChooseRushAction(team, carrier, out Position receiver, out GridPoint target);
            double duration = action == RushAction.Shoot
                ? Seconds("shoot") + Seconds("rebound")
                : Seconds(action == RushAction.Pass ? "pass" : "skate");
            if (WouldCross(duration))
            {
                // O-1: nothing of a sequence that would cross the period end happens.
                _state.RestoreNodes(saved);
                pinch.CopyTo(_pinchActive, 0);
                EndPeriod();
                return;
            }

            if (action == RushAction.Skate && target.Equals(TeamView(team, team, carrier)))
            {
                _state.RestoreNodes(saved);
                pinch.CopyTo(_pinchActive, 0);
                SelectNext(_tuning.Time.SetupSeconds);
                return;
            }

            CaptureRates();
            _rushActions++;
            switch (action)
            {
                case RushAction.Shoot:
                    Shoot(team, carrier);
                    break;
                case RushAction.Pass:
                    Pass(team, carrier, receiver);
                    break;
                default:
                    Skate(team, carrier, target);
                    break;
            }
        }

        /// <summary>O-7 support: forwards to (min(carrier x, goal x − 1), own lane), defence to (max(0, min(carrier x − 1, centre line)), own side).</summary>
        private void MoveRushSupport(TeamSide team)
        {
            Position carrier = _state.CarrierPosition.Value;
            int carrierX = TeamView(team, team, carrier).X;
            int lane = _rink.CentreLaneY;
            int forwardX = System.Math.Min(carrierX, _rink.OpponentGoal.X - 1);
            int defenceX = System.Math.Max(0, System.Math.Min(carrierX - 1, _rink.CentreLineX));
            var targets = new List<KeyValuePair<Position, GridPoint>>();
            AddSupport(targets, carrier, Position.LeftWing, new GridPoint(forwardX, lane - 1));
            AddSupport(targets, carrier, Position.Center, new GridPoint(forwardX, lane));
            AddSupport(targets, carrier, Position.RightWing, new GridPoint(forwardX, lane + 1));
            AddSupport(targets, carrier, Position.LeftDefence, new GridPoint(defenceX, lane - 1));
            AddSupport(targets, carrier, Position.RightDefence, new GridPoint(defenceX, lane + 1));
            MoveSkaters(team, targets, _tuning.Plays.MaxNodesPerBeat);
        }

        private void AddSupport(List<KeyValuePair<Position, GridPoint>> targets, Position carrier, Position position, GridPoint target)
        {
            if (position == carrier)
            {
                return;
            }

            // A support target on a goal node (e.g. C behind the own goal line) moves to that goal's net front (D-033).
            GridPoint node = _rink.IsGoalNode(target) ? _rink.NetFrontOf(target) : target;
            targets.Add(new KeyValuePair<Position, GridPoint>(position, node));
        }

        /// <summary>O-7 carrier action: shoot from a shooting node, else pass to a teammate on one with a free lane, else skate.</summary>
        private RushAction ChooseRushAction(TeamSide team, Position carrier, out Position receiver, out GridPoint target)
        {
            receiver = carrier;
            GridPoint from = TeamView(team, team, carrier);
            target = from;
            List<GridPoint> shootingNodes = RushShootingNodes();
            if (shootingNodes.Contains(from))
            {
                return RushAction.Shoot;
            }

            IReadOnlyDictionary<Position, GridPoint> defenders = NodesInView(TeamSides.Opponent(team), team);
            bool found = false;
            double bestXg = 0.0;
            int bestDistance = 0;
            foreach (Position position in PositionOrder.All)
            {
                GridPoint node = TeamView(team, team, position);
                if (position == carrier || !shootingNodes.Contains(node))
                {
                    continue;
                }

                LineGeometry.LineDefender(defenders, LineGeometry.PassLane(from, node), node, out Position unused, out int laneDistance);
                if (laneDistance < 1)
                {
                    continue;
                }

                double xg = BaseXgAt(node);
                int distance = GridPoint.Chebyshev(from, node);
                if (!found || xg > bestXg || (xg == bestXg && distance < bestDistance))
                {
                    found = true;
                    receiver = position;
                    bestXg = xg;
                    bestDistance = distance;
                }
            }

            if (found)
            {
                return RushAction.Pass;
            }

            GridPoint goalward;
            if (_rink.ZoneAtX(from.X) != RinkZone.Offensive)
            {
                goalward = new GridPoint(_rink.Length - 1, from.Y);
            }
            else
            {
                goalward = NearestShootingNode(from, shootingNodes);
            }

            target = SystemMovement.Advance(from, goalward, from, _tuning.Plays.MaxNodesPerBeat, _rink);
            return RushAction.Skate;
        }

        /// <summary>O-7 S: offensive-zone nodes (attacker's view) with baseXg at least <c>transitions.rushShotMinBaseXg</c>, goal excluded.</summary>
        private List<GridPoint> RushShootingNodes()
        {
            var nodes = new List<GridPoint>();
            for (int x = 0; x < _rink.Length; x++)
            {
                for (int y = 0; y < _rink.Width; y++)
                {
                    var node = new GridPoint(x, y);
                    if (_rink.ZoneAtX(x) == RinkZone.Offensive && !_rink.IsGoalNode(node) && BaseXgAt(node) >= _tuning.Transitions.RushShotMinBaseXg)
                    {
                        nodes.Add(node);
                    }
                }
            }

            return nodes;
        }

        private double BaseXgAt(GridPoint node)
        {
            return _tuning.Shot.BaseXg[_rink.GetNode(node.X, node.Y).XgZone];
        }

        /// <summary>Nearest shooting node: Chebyshev, Manhattan, higher baseXg, closer to the middle lane, smaller y.</summary>
        private GridPoint NearestShootingNode(GridPoint from, List<GridPoint> nodes)
        {
            GridPoint best = from;
            bool found = false;
            foreach (GridPoint node in nodes)
            {
                if (!found || CloserShootingNode(from, node, best))
                {
                    best = node;
                    found = true;
                }
            }

            return best;
        }

        private bool CloserShootingNode(GridPoint from, GridPoint a, GridPoint b)
        {
            int c = GridPoint.Chebyshev(from, a).CompareTo(GridPoint.Chebyshev(from, b));
            if (c == 0)
            {
                c = GridPoint.Manhattan(from, a).CompareTo(GridPoint.Manhattan(from, b));
            }

            if (c == 0)
            {
                c = -BaseXgAt(a).CompareTo(BaseXgAt(b));
            }

            if (c == 0)
            {
                c = System.Math.Abs(a.Y - _rink.CentreLaneY).CompareTo(System.Math.Abs(b.Y - _rink.CentreLaneY));
            }

            if (c == 0)
            {
                c = a.Y.CompareTo(b.Y);
            }

            return c < 0;
        }

        /// <summary>O-7 regroup: regroupSeconds without checks or attacker movement, then a play with a longer defence setup.</summary>
        private void RegroupStep()
        {
            if (!Begin(_tuning.Time.RegroupSeconds))
            {
                return;
            }

            Spend(_tuning.Time.RegroupSeconds);
            SelectNext(_tuning.Time.RegroupSeconds + _tuning.Time.SetupSeconds);
        }
    }
}
