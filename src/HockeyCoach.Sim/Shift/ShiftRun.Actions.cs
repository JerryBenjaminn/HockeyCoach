using System.Collections.Generic;
using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>Puck actions of a beat: skate, pass and drive the net (D-032, D-039, D-040).</summary>
    internal sealed partial class ShiftRun
    {
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
            Spend("skate");
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

            Spend("pass");
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
            Spend("driveNet");
            ClearActionHistory();
        }
    }
}
