using System.Collections.Generic;
using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
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

            TraceSnapshotS(team);
            IReadOnlyDictionary<Position, GridPoint>[] saved = _state.SaveNodes();
            bool[] pinch = (bool[])_pinchActive.Clone();
            MoveRushSupport(team);
            ApplyActionDefence(team, PresserSource.Nearest);
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
                    Shoot(team, carrier, PresserSource.Nearest);
                    break;
                case RushAction.Pass:
                    Pass(team, carrier, receiver, PresserSource.Nearest);
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

        /// <summary>
        /// O-7 regroup: regroupSeconds without attacker movement, then a play with a longer defence setup. In the own zone the
        /// defence moves its regroup steps first and enough forecheckers challenge the regroup with a breakout check
        /// (D-078 K1); elsewhere the regroup has no check.
        /// </summary>
        private void RegroupStep()
        {
            double regroupSeconds = _tuning.Time.RegroupSeconds;
            if (!Begin(regroupSeconds))
            {
                return;
            }

            _presserDelayPending = false;
            TeamSide team = _attacker;
            Position carrier = _state.CarrierPosition.Value;
            if (_rink.ZoneAtX(TeamView(team, team, carrier).X) != RinkZone.Defensive)
            {
                Spend(regroupSeconds);
                SelectNext(regroupSeconds + _tuning.Time.SetupSeconds);
                return;
            }

            int totalSteps = SetupSteps(regroupSeconds + _tuning.Time.SetupSeconds);
            int regroupSteps = SetupSteps(regroupSeconds);
            ApplyDefence(TeamSides.Opponent(team), regroupSteps, DefenceMoveKind.Regroup);
            Spend(regroupSeconds);
            IReadOnlyList<Position> forecheckers = ForecheckersInZone(team, out IReadOnlyDictionary<SystemRole, Position> unused);
            if (RegroupChallenge.IsChallenged(forecheckers.Count, _tuning.Transitions.RegroupForecheckersAllowed)
                && !RegroupBreakout(team, carrier, forecheckers))
            {
                return;
            }

            _setupSteps = RegroupChallenge.SetupStepsAfterRegroup(totalSteps, regroupSteps, Unbounded);
            _mode = Mode.SelectPlay;
        }

        /// <summary>
        /// O-7 point 4-5 (D-078 K1): the carrier's breakout check against the forecheckers (M-1), with the usual breakout
        /// modifiers and no familiarity penalty or action time. Failure: the nearest forechecker takes the puck as in a
        /// failed skate. Returns true when the regroup goes on.
        /// </summary>
        private bool RegroupBreakout(TeamSide team, Position carrier, IReadOnlyList<Position> forecheckers)
        {
            TeamSide defending = TeamSides.Opponent(team);
            CheckDefinition check = _tuning.GetCheck("breakout");
            Skater carrierSkater = _state.SkaterAt(team, carrier);
            Skater[] attackerSide = { carrierSkater };
            Skater[] defenderSide = Forecheckers(team);
            double m = OffSide(attackerSide, team, defenderSide, defending)
                + StateModifier(check.Modifiers, defending, attackerSide, defenderSide);
            CheckResult result = CheckResolver.Resolve(
                _tuning.CheckFormula,
                check,
                new CheckParticipants().With("carrier", carrierSkater).With("forecheckers", defenderSide),
                m,
                _random);
            Observe(result, team, attackerSide, defenderSide);
            PayCost(attackerSide);
            PayCost(defenderSide);
            ClearActionHistory();
            _lastAction[(int)team] = LastPuckAction.Other;
            if (result.Success)
            {
                return true;
            }

            Position taker = RegroupChallenge.NearestForechecker(forecheckers, NodesInView(defending, team), TeamView(team, team, carrier));
            _state.GivePuckTo(defending, taker);
            Takeaway(team, carrier, defending, taker, GainKind.Takeaway);
            return false;
        }
    }
}
