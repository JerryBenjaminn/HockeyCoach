using System.Collections.Generic;
using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// The shot sequence (Q-010): block (M-5) → on target (<c>onTargetShare</c>) → goal check → rebound. Reported xG is
    /// the attempt's total probability: through × on target × goal (Q-014 default, D-046).
    /// </summary>
    internal sealed partial class ShiftRun
    {
        private void Shoot(TeamSide team, Position shooterPosition)
        {
            TeamSide defending = TeamSides.Opponent(team);
            GridPoint shooterNode = TeamView(team, team, shooterPosition);
            GridPoint goal = _rink.OpponentGoal;
            GridPoint netFront = _rink.NetFrontOf(goal);
            IReadOnlyDictionary<Position, GridPoint> defenders = NodesInView(defending, team);
            Skater shooter = _state.SkaterAt(team, shooterPosition);
            Goalie goalie = Team(defending).Goalie;
            ShotConfig shotConfig = _tuning.Shot;

            bool underPressure = UnderPressure(shooterNode, defenders);
            string xgZone = UsesCrease(team, shooterPosition, shooterNode, netFront) ? "crease" : _rink.GetNode(shooterNode.X, shooterNode.Y).XgZone;
            double goalModifier = (RoyalRoad(team, shooterPosition) ? shotConfig.Modifiers.Get("royalRoad") : 0.0)
                + (Screen(team, shooterPosition, shooterNode, netFront, goal) ? shotConfig.Modifiers.Get("screen") : 0.0)
                + OffSide(new[] { shooter }, team, new Skater[0], defending);
            var shotParticipants = new CheckParticipants().With("shooter", shooter).With(CheckRoles.Goalie, goalie);
            double goalProbability = ShotResolver.GoalProbability(_tuning.CheckFormula, shotConfig, xgZone, shotParticipants, goalModifier);

            // Block (M-5).
            var shotLine = new List<GridPoint>();
            foreach (GridPoint node in LineGeometry.Nodes(shooterNode, goal))
            {
                if (!node.Equals(goal))
                {
                    shotLine.Add(node);
                }
            }

            CheckDefinition block = _tuning.GetCheck("block");
            int maxLane = (int)block.GetParameter("maxLaneDistance");
            double through = 1.0;
            bool blocked = false;
            Position blocker = default;
            if (LineGeometry.Closest(defenders, shotLine, shooterNode, maxLane, out blocker, out int laneDistance))
            {
                Skater blockerSkater = _state.SkaterAt(defending, blocker);
                double m = block.Modifiers.Get("distancePerNode") * GridPoint.Chebyshev(shooterNode, defenders[blocker])
                    + block.Modifiers.GetTable("laneDistance", laneDistance)
                    + OffSide(new Skater[0], team, new[] { blockerSkater }, defending);
                CheckResult blockResult = CheckResolver.Resolve(
                    _tuning.CheckFormula,
                    block,
                    new CheckParticipants().With("nearestDefender", blockerSkater),
                    m,
                    _random);
                through = blockResult.Probability;
                blocked = !blockResult.Success;
            }

            double xg = through * shotConfig.OnTargetShare * goalProbability;
            ChanceType chanceType = ChanceTypeOf(team);
            ChanceClass? chanceClass = ClassOf(xg);
            Spend("shoot");
            ClearActionHistory();

            if (blocked)
            {
                _state.SetLoosePuck(TeamFrame.ToRink(LooseSpot(_tuning.LoosePuckSpots.BlockedShot, shooterNode, defenders[blocker]), team, _rink));
                _log.Append(new ShotEvent(Context(), shooter.Id, _state.PlayerId(defending, blocker), xg, chanceClass, chanceType, underPressure, null, ShotOutcome.Blocked));
                _play = null;
                StartLooseBattle(team, LooseKind.Normal, null);
                return;
            }

            if (!_random.Chance(shotConfig.OnTargetShare))
            {
                _state.SetLoosePuck(TeamFrame.ToRink(LooseSpot(_tuning.LoosePuckSpots.MissedShot, shooterNode, null), team, _rink));
                _log.Append(new ShotEvent(Context(), shooter.Id, null, xg, chanceClass, chanceType, underPressure, null, ShotOutcome.Missed));
                _play = null;
                StartLooseBattle(team, LooseKind.Normal, null);
                return;
            }

            bool scored = _random.Chance(goalProbability);
            if (scored)
            {
                _goals[(int)team]++;
                _log.Append(new ShotEvent(Context(), shooter.Id, null, xg, chanceClass, chanceType, underPressure, null, ShotOutcome.Goal));
                _play = null;
                End(ShiftEndReason.Goal, StoppageReason.Goal);
                return;
            }

            _log.Append(new ShotEvent(Context(), shooter.Id, goalie.Id, xg, chanceClass, chanceType, underPressure, null, ShotOutcome.Saved));
            _play = null;
            Rebound(team, shooter, shooterNode);
        }

        /// <summary>After a save: rebound to the slot, held (stoppage) or to the corner (D-044).</summary>
        private void Rebound(TeamSide team, Skater shooter, GridPoint shooterNode)
        {
            TeamSide defending = TeamSides.Opponent(team);
            CheckDefinition rebound = _tuning.GetCheck("rebound");
            double m = rebound.Modifiers.Get("shotPowerPerPoint") * (shooter.Stats[SkaterStat.ShotPower] - _tuning.CheckFormula.ReferenceValue);
            CheckResult result = CheckResolver.Resolve(
                _tuning.CheckFormula,
                rebound,
                new CheckParticipants().With(CheckRoles.Goalie, Team(defending).Goalie),
                m,
                _random);
            Spend("rebound");
            if (result.Success)
            {
                _state.SetLoosePuck(TeamFrame.ToRink(LooseSpot(_tuning.LoosePuckSpots.ReboundSlot, shooterNode, null), team, _rink));
                StartLooseBattle(team, LooseKind.Normal, null);
                return;
            }

            if (_random.Chance(rebound.GetParameter("controlledHoldShare")))
            {
                End(ShiftEndReason.GoalieFreeze, StoppageReason.GoalieFreeze);
                return;
            }

            _state.SetLoosePuck(TeamFrame.ToRink(LooseSpot(_tuning.LoosePuckSpots.ReboundCorner, shooterNode, null), team, _rink));
            StartLooseBattle(team, LooseKind.Normal, null);
        }

        /// <summary>
        /// A loose-puck spot (D-044, attacker's view). <paramref name="actorNode"/> is the shooter's node;
        /// <paramref name="reference"/> the blocker's or lane defender's node where the situation has one (the validator
        /// guarantees it is there when the rule needs it). <c>shooterSideCorner</c> from the middle lane draws one 50/50 value.
        /// </summary>
        private GridPoint LooseSpot(LoosePuckRule rule, GridPoint actorNode, GridPoint? reference)
        {
            GridPoint goal = _rink.OpponentGoal;
            switch (rule)
            {
                case LoosePuckRule.NetFront:
                    return _rink.NetFrontOf(goal);
                case LoosePuckRule.ShooterSideCorner:
                    Lane lane = _rink.LaneOf(actorNode.Y);
                    bool left = lane == Lane.Left || (lane == Lane.Middle && _random.Chance(0.5));
                    return new GridPoint(goal.X, left ? 0 : _rink.Width - 1);
                case LoosePuckRule.EndRowShooterLane:
                    return new GridPoint(_rink.Length - 1, actorNode.Y);
                default:
                    return reference ?? throw new System.InvalidOperationException("Rule " + rule + " needs a reference node here.");
            }
        }

        /// <summary>M-3 Royal Road.</summary>
        private bool RoyalRoad(TeamSide team, Position shooter)
        {
            LastPass pass = _lastPass;
            if (pass == null || pass.Team != team || pass.To != shooter || !pass.CrossIce)
            {
                return false;
            }

            int offensiveMin = int.MaxValue;
            foreach (ZoneRange zone in _rink.Zones)
            {
                if (zone.Zone == RinkZone.Offensive)
                {
                    offensiveMin = zone.XMin;
                }
            }

            int goalX = _rink.OpponentGoal.X;
            bool inRange = pass.PasserNode.X >= offensiveMin && pass.PasserNode.X <= goalX
                && pass.ReceiverNode.X >= offensiveMin && pass.ReceiverNode.X <= goalX;
            bool deep = pass.PasserNode.X >= goalX - 1 || pass.ReceiverNode.X >= goalX - 1;
            return inRange && deep;
        }

        /// <summary>M-4 screen.</summary>
        private bool Screen(TeamSide team, Position shooter, GridPoint shooterNode, GridPoint netFront, GridPoint goal)
        {
            if (shooterNode.Equals(netFront) || shooterNode.X >= goal.X)
            {
                return false;
            }

            foreach (Position position in PositionOrder.All)
            {
                if (position != shooter && TeamView(team, team, position).Equals(netFront))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// D-047, D-059: from the net front, a second-chance shot after a loose puck won there, or a pass to a player who
        /// drove the net.
        /// </summary>
        private bool UsesCrease(TeamSide team, Position shooter, GridPoint shooterNode, GridPoint netFront)
        {
            if (!shooterNode.Equals(netFront))
            {
                return false;
            }

            bool tip = _droveNet[(int)team, (int)shooter] && _lastPass != null && _lastPass.Team == team && _lastPass.To == shooter;
            return _secondChance || tip;
        }

        /// <summary>Faceoff play → faceoff; a takeaway in the neutral or offensive zone within the window → turnover; else offensive zone.</summary>
        private ChanceType ChanceTypeOf(TeamSide team)
        {
            if (_play != null && _play.Type == PlayType.Faceoff)
            {
                return ChanceType.Faceoff;
            }

            double takeaway = _takeawayTime[(int)team];
            if (!double.IsNaN(takeaway) && _time - takeaway <= _tuning.ChanceTypes.TurnoverWindowSeconds)
            {
                GridPoint node = TeamFrame.ToTeamView(_takeawayNode[(int)team], team, _rink);
                if (_rink.ZoneAtX(node.X) != RinkZone.Defensive)
                {
                    return ChanceType.Turnover;
                }
            }

            return ChanceType.OffensiveZone;
        }

        private ChanceClass? ClassOf(double xg)
        {
            ChanceClassesConfig classes = _tuning.ChanceClasses;
            if (xg >= classes.TopMinXg)
            {
                return ChanceClass.Top;
            }

            if (xg >= classes.GoodMinXg)
            {
                return ChanceClass.Good;
            }

            return xg >= classes.ModerateMinXg ? ChanceClass.Moderate : (ChanceClass?)null;
        }
    }
}
