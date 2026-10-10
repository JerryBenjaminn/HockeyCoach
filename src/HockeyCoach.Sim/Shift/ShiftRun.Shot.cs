using System.Collections.Generic;
using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.State;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// The shot sequence (Q-010, O-13): block (M-5) → on target (<c>onTargetShare</c>) → out of play or loose for a miss
    /// (D-063) / goal check → rebound. Reported xG is the attempt's total probability: through × on target × goal
    /// (Q-014 default, D-046).
    /// </summary>
    internal sealed partial class ShiftRun
    {
        private void Shoot(TeamSide team, Position shooterPosition)
        {
            TrackEntry();
            TeamSide defending = TeamSides.Opponent(team);
            GridPoint shooterNode = TeamView(team, team, shooterPosition);
            GridPoint goal = _rink.OpponentGoal;
            GridPoint netFront = _rink.NetFrontOf(goal);
            IReadOnlyDictionary<Position, GridPoint> defenders = NodesInView(defending, team);
            Skater shooter = _state.SkaterAt(team, shooterPosition);
            Goalie goalie = GoalieOf(defending);
            ShotConfig shotConfig = _tuning.Shot;
            _hasLastShot = true;
            _lastShotTeam = team;
            _lastShotNode = _state.NodeOf(team, shooterPosition);

            bool underPressure = UnderPressure(shooterNode, defenders);
            bool crease = UsesCrease(team, shooterPosition, shooterNode, netFront);
            string xgZone = crease ? "crease" : _rink.GetNode(shooterNode.X, shooterNode.Y).XgZone;
            bool royalRoad = RoyalRoad(team, shooterPosition);
            double screen = ScreenModifier(team, shooterPosition, shooterNode, netFront, goal, defenders);
            bool secondChance = _secondChance;
            double defenceOrganization = _game.Organization(defending);
            double goalModifier = (royalRoad ? shotConfig.Modifiers.Get("royalRoad") : 0.0)
                + screen
                + OffSide(new[] { shooter }, team, new Skater[0], defending)
                + OrganizationModifier(shotConfig.Modifiers, defending)
                + EnergyModifier(new[] { shooter.Id }, new[] { goalie.Id })
                + StateDynamics.PressureModifier(shotConfig.Modifiers.Get("pressure"), _game.Pressure(team), GoalieFactor(goalie));
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
                    + OffSide(new Skater[0], team, new[] { blockerSkater }, defending)
                    + OrganizationModifier(block.Modifiers, defending)
                    + EnergyModifier(new int[0], new[] { blockerSkater.Id })
                    - PlayFamiliarity();
                CheckResult blockResult = CheckResolver.Resolve(
                    _tuning.CheckFormula,
                    block,
                    new CheckParticipants().With("nearestDefender", blockerSkater),
                    m,
                    _random);
                PayCost(blockerSkater);
                through = blockResult.Probability;
                blocked = !blockResult.Success;
            }

            double xg = through * shotConfig.OnTargetShare * goalProbability;
            ChanceType chanceType = ChanceTypeOf(team);
            ChanceClass? chanceClass = ClassOf(xg);
            PayCost(shooter);
            GainPressure(team, _tuning.Pressure.GainOnShot);
            Spend("shoot");
            ClearActionHistory();
            _lastAction[(int)team] = LastPuckAction.ShotOrDump;

            if (blocked)
            {
                _state.SetLoosePuck(TeamFrame.ToRink(LooseSpot(_tuning.LoosePuckSpots.BlockedShot, shooterNode, defenders[blocker]), team, _rink));
                _log.Append(new ShotEvent(Context(), shooter.Id, _state.PlayerId(defending, blocker), xg, chanceClass, chanceType, underPressure, null, ShotOutcome.Blocked));
                _play = null;
                NetFrontAfterShot(team, shooterPosition);
                StartLooseBattle(team, LooseKind.Normal, null);
                return;
            }

            if (!_random.Chance(shotConfig.OnTargetShare))
            {
                bool outOfPlay = _random.Chance(shotConfig.Parameters[MissedOutOfPlayShare]);
                if (!outOfPlay)
                {
                    _state.SetLoosePuck(TeamFrame.ToRink(LooseSpot(_tuning.LoosePuckSpots.MissedShot, shooterNode, null), team, _rink));
                }

                _log.Append(new ShotEvent(Context(), shooter.Id, null, xg, chanceClass, chanceType, underPressure, null, ShotOutcome.Missed));
                _play = null;
                if (outOfPlay)
                {
                    End(ShiftEndReason.OutOfPlay, StoppageReason.OutOfPlay);
                    return;
                }

                NetFrontAfterShot(team, shooterPosition);
                StartLooseBattle(team, LooseKind.Normal, null);
                return;
            }

            bool scored = _random.Chance(goalProbability);
            if (scored)
            {
                _goals[(int)team]++;
                _log.Append(new ShotEvent(Context(), shooter.Id, null, xg, chanceClass, chanceType, underPressure, null, ShotOutcome.Goal));
                bool hasEntry = _hasPossession && _possessionTeam == team && _hasEntryNumbers;
                _game.AddGoalNote(new GoalNote(
                    _setup.Period, _time, team, shooter.Id, chanceType, defenceOrganization, hasEntry, hasEntry ? _entryNumbers : default, royalRoad, screen != 0.0, crease, secondChance));
                _play = null;
                End(ShiftEndReason.Goal, StoppageReason.Goal);
                return;
            }

            _log.Append(new ShotEvent(Context(), shooter.Id, goalie.Id, xg, chanceClass, chanceType, underPressure, null, ShotOutcome.Saved));
            _play = null;
            NetFrontAfterShot(team, shooterPosition);
            Rebound(team, shooter, shooterNode);
        }

        /// <summary><c>checks.shot.missedOutOfPlayShare</c> (D-063).</summary>
        internal const string MissedOutOfPlayShare = "missedOutOfPlayShare";

        /// <summary>After a save: rebound to the slot, held (stoppage) or to the corner (D-044).</summary>
        private void Rebound(TeamSide team, Skater shooter, GridPoint shooterNode)
        {
            TeamSide defending = TeamSides.Opponent(team);
            Goalie goalie = GoalieOf(defending);
            CheckDefinition rebound = _tuning.GetCheck("rebound");

            // O-5: the rebound uses only its own goalieEnergy modifier, not the general energy term.
            double m = rebound.Modifiers.Get("shotPowerPerPoint") * (shooter.Stats[SkaterStat.ShotPower] - _tuning.CheckFormula.ReferenceValue)
                + (rebound.Modifiers.Get("goalieEnergy") * (1.0 - _game.Energy.Get(goalie.Id)));
            CheckResult result = CheckResolver.Resolve(
                _tuning.CheckFormula,
                rebound,
                new CheckParticipants().With(CheckRoles.Goalie, goalie),
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
        /// O-10 <c>netFrontAfterShot</c>: the shooting team's nearest forward (not the shooter, not already at the net
        /// front) moves up to <c>plays.maxNodesPerBeat</c> toward the net front and is committed. No time.
        /// </summary>
        private void NetFrontAfterShot(TeamSide team, Position shooter)
        {
            if (!Team(team).Plan.Instructions.NetFrontAfterShot)
            {
                return;
            }

            GridPoint netFront = _rink.NetFrontOf(_rink.OpponentGoal);
            var candidates = new SortedDictionary<Position, GridPoint>();
            foreach (Position position in PositionOrder.Forwards)
            {
                GridPoint node = TeamView(team, team, position);
                if (position != shooter && !node.Equals(netFront))
                {
                    candidates.Add(position, node);
                }
            }

            if (!LineGeometry.Closest(candidates, new[] { netFront }, netFront, int.MaxValue, out Position mover, out int unused))
            {
                return;
            }

            MoveSkaters(team, new[] { new KeyValuePair<Position, GridPoint>(mover, netFront) }, _tuning.Plays.MaxNodesPerBeat);
            _committed[(int)team, (int)mover] = true;
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

        /// <summary>M-4 screen; <c>screenContested</c> instead when a defender is on the net front (Q-031, D-062).</summary>
        private double ScreenModifier(TeamSide team, Position shooter, GridPoint shooterNode, GridPoint netFront, GridPoint goal, IReadOnlyDictionary<Position, GridPoint> defenders)
        {
            if (shooterNode.Equals(netFront) || shooterNode.X >= goal.X)
            {
                return 0.0;
            }

            bool screened = false;
            foreach (Position position in PositionOrder.All)
            {
                if (position != shooter && TeamView(team, team, position).Equals(netFront))
                {
                    screened = true;
                }
            }

            if (!screened)
            {
                return 0.0;
            }

            foreach (KeyValuePair<Position, GridPoint> defender in defenders)
            {
                if (defender.Value.Equals(netFront))
                {
                    return _tuning.Shot.Modifiers.Get("screenContested");
                }
            }

            return _tuning.Shot.Modifiers.Get("screen");
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

        /// <summary>
        /// O-12, first match: faceoff (the faceoff play, uninterrupted since the draw); turnover (last gain from the opponent
        /// in the neutral or offensive zone within <c>turnoverWindowSeconds</c>, Q-039 default before rush); rush (the
        /// possession started outside the offensive zone, entered it, and the defence is below
        /// <c>organization.organizedThreshold</c>); otherwise offensive zone.
        /// </summary>
        private ChanceType ChanceTypeOf(TeamSide team)
        {
            if (_play != null && _play.Type == PlayType.Faceoff)
            {
                return ChanceType.Faceoff;
            }

            double gain = _gainTime[(int)team];
            if (!double.IsNaN(gain) && _time - gain <= _tuning.ChanceTypes.TurnoverWindowSeconds + TimeEpsilon
                && _gainZone[(int)team] != RinkZone.Defensive)
            {
                return ChanceType.Turnover;
            }

            bool samePossession = _hasPossession && _possessionTeam == team;
            if (samePossession && _possessionStartZone != RinkZone.Offensive && _enteredZone
                && _game.Organization(TeamSides.Opponent(team)) < _tuning.Organization.OrganizedThreshold)
            {
                return ChanceType.Rush;
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
