using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// Validates a play against docs/data-schema.md (Kuviot → Validointi; D-031..D-034). File-level rules (schema
    /// version, unknown fields, id = file name, unique ids, known names) are checked by the loader. The validator walks
    /// the beats, tracking every position's node and the puck carrier, and returns every problem with its data path.
    /// </summary>
    public static class PlayValidator
    {
        private static readonly Position[] AllPositions =
        {
            Position.Center, Position.LeftWing, Position.RightWing, Position.LeftDefence, Position.RightDefence,
        };

        /// <summary>Returns every problem found; empty means valid.</summary>
        public static IReadOnlyList<string> Validate(Play play, Rink rink, PlaysConfig plays)
        {
            var errors = new List<string>();
            if (play == null || rink == null || plays == null)
            {
                errors.Add("(root): play, rink and plays config are required");
                return errors;
            }

            ValidateType(play, rink, errors);
            bool startOk = ValidateStart(play, rink, errors);

            if (play.Beats.Count == 0 || play.Beats.Count > plays.MaxBeats)
            {
                errors.Add("beats: must have 1.." + plays.MaxBeats + " beats (plays.maxBeats), has " + play.Beats.Count);
            }

            if (startOk)
            {
                WalkBeats(play, rink, plays, errors);
            }

            return errors;
        }

        private static void ValidateType(Play play, Rink rink, List<string> errors)
        {
            if (play.Type == PlayType.Faceoff)
            {
                if (play.FaceoffSpotId == null)
                {
                    errors.Add("faceoffSpot: missing (faceoff play)");
                }
                else if (!HasSpot(rink, play.FaceoffSpotId))
                {
                    errors.Add("faceoffSpot: " + play.FaceoffSpotId + " is not a faceoff spot in rink.json");
                }
            }
            else if (play.FaceoffSpotId != null)
            {
                errors.Add("faceoffSpot: only faceoff plays have a faceoff spot");
            }
        }

        private static bool ValidateStart(Play play, Rink rink, List<string> errors)
        {
            bool ok = true;
            foreach (Position position in AllPositions)
            {
                if (!play.StartPositions.ContainsKey(position))
                {
                    errors.Add("start.positions." + Positions.ToName(position) + ": missing (all of LW, C, RW, LD, RD are required)");
                    ok = false;
                }
            }

            foreach (KeyValuePair<Position, GridPoint> start in play.StartPositions)
            {
                ok &= ValidateSkaterNode("start.positions." + Positions.ToName(start.Key), start.Value, rink, errors);
            }

            if (ok)
            {
                ValidateDistinctNodes("start.positions", play.StartPositions, errors);
            }

            return ok;
        }

        private static void WalkBeats(Play play, Rink rink, PlaysConfig plays, List<string> errors)
        {
            var nodes = new SortedDictionary<Position, GridPoint>();
            foreach (KeyValuePair<Position, GridPoint> start in play.StartPositions)
            {
                nodes.Add(start.Key, start.Value);
            }

            Position carrier = play.PuckCarrier;
            GridPoint netFront = rink.NetFrontOf(rink.OpponentGoal);
            for (int i = 0; i < play.Beats.Count; i++)
            {
                string path = "beats[" + i + "]";
                Beat beat = play.Beats[i];
                PlayAction action = beat.Action;
                if (i > 0 && play.Beats[i - 1].Action.EndsPlay)
                {
                    errors.Add(path + ": no beats may follow " + PlayNames.ToName(play.Beats[i - 1].Action.Type));
                }

                foreach (KeyValuePair<Position, GridPoint> move in beat.Moves)
                {
                    string movePath = path + ".moves." + Positions.ToName(move.Key);
                    if (move.Key == carrier)
                    {
                        errors.Add(movePath + ": the puck carrier moves only with skate (D-032)");
                    }

                    if (action.Type == PlayActionType.DriveNet && move.Key == action.Actor)
                    {
                        errors.Add(movePath + ": the driveNet player of the same beat must not move");
                    }

                    if (!ValidateSkaterNode(movePath, move.Value, rink, errors))
                    {
                        continue;
                    }

                    if (move.Value.Equals(netFront))
                    {
                        errors.Add(movePath + ": a move must not end at the net front " + netFront + "; use driveNet");
                    }

                    ValidateReach(movePath, nodes[move.Key], move.Value, plays, errors);
                    nodes[move.Key] = move.Value;
                }

                carrier = ApplyAction(path + ".action", action, carrier, nodes, netFront, rink, plays, errors);
                ValidateDistinctNodes(path, nodes, errors);
            }
        }

        private static Position ApplyAction(
            string path,
            PlayAction action,
            Position carrier,
            SortedDictionary<Position, GridPoint> nodes,
            GridPoint netFront,
            Rink rink,
            PlaysConfig plays,
            List<string> errors)
        {
            string actorName = Positions.ToName(action.Actor);
            if (action.ActorHasPuck && action.Actor != carrier)
            {
                errors.Add(path + (action.Type == PlayActionType.Pass ? ".from" : ".by") + ": " + actorName
                    + " is not the puck carrier at the start of the beat (" + Positions.ToName(carrier) + ")");
            }

            switch (action.Type)
            {
                case PlayActionType.Skate:
                    if (ValidateSkaterNode(path + ".to", action.Target.Value, rink, errors))
                    {
                        ValidateReach(path + ".to", nodes[action.Actor], action.Target.Value, plays, errors);
                        nodes[action.Actor] = action.Target.Value;
                    }

                    return carrier;

                case PlayActionType.Pass:
                    if (action.Receiver.Value == action.Actor)
                    {
                        errors.Add(path + ".to: must differ from from");
                        return carrier;
                    }

                    return action.Receiver.Value;

                case PlayActionType.DriveNet:
                    GridPoint from = nodes[action.Actor];
                    if (action.Actor == carrier)
                    {
                        errors.Add(path + ".by: " + actorName + " has the puck; driveNet is for a player without the puck (D-020)");
                    }
                    else if (from.Equals(netFront))
                    {
                        errors.Add(path + ".by: " + actorName + " is already at the net front " + netFront);
                    }
                    else if (GridPoint.Chebyshev(from, netFront) > plays.MaxNodesPerBeat)
                    {
                        errors.Add(path + ".by: " + actorName + " at " + from + " is more than plays.maxNodesPerBeat ("
                            + plays.MaxNodesPerBeat + ") nodes from the net front " + netFront);
                    }

                    nodes[action.Actor] = netFront;
                    return carrier;

                case PlayActionType.Dump:
                    if (ValidateSkaterNode(path + ".to", action.Target.Value, rink, errors)
                        && rink.ZoneAtX(action.Target.Value.X) != RinkZone.Offensive)
                    {
                        errors.Add(path + ".to: " + action.Target.Value + " is not in the offensive zone");
                    }

                    if (nodes[action.Actor].X < rink.CentreLineX)
                    {
                        errors.Add(path + ".by: " + actorName + " at " + nodes[action.Actor] + " is behind the centre line (x < "
                            + rink.CentreLineX + "); a long dump is not a play action");
                    }

                    return carrier;

                default:
                    return carrier;
            }
        }

        private static bool ValidateSkaterNode(string path, GridPoint node, Rink rink, List<string> errors)
        {
            if (!rink.Contains(node))
            {
                errors.Add(path + ": " + node + " is outside the " + rink.Length + "x" + rink.Width + " grid");
                return false;
            }

            if (rink.IsGoalNode(node))
            {
                errors.Add(path + ": " + node + " is a goal node; skaters never stand there (D-033)");
                return false;
            }

            return true;
        }

        private static void ValidateReach(string path, GridPoint from, GridPoint to, PlaysConfig plays, List<string> errors)
        {
            int distance = GridPoint.Chebyshev(from, to);
            if (distance > plays.MaxNodesPerBeat)
            {
                errors.Add(path + ": " + from + " → " + to + " is " + distance + " nodes, more than plays.maxNodesPerBeat (" + plays.MaxNodesPerBeat + ")");
            }
        }

        private static void ValidateDistinctNodes(string path, IReadOnlyDictionary<Position, GridPoint> nodes, List<string> errors)
        {
            var seen = new Dictionary<GridPoint, Position>();
            foreach (KeyValuePair<Position, GridPoint> entry in nodes)
            {
                if (seen.TryGetValue(entry.Value, out Position other))
                {
                    errors.Add(path + ": " + Positions.ToName(other) + " and " + Positions.ToName(entry.Key) + " are both at " + entry.Value);
                }
                else
                {
                    seen.Add(entry.Value, entry.Key);
                }
            }
        }

        private static bool HasSpot(Rink rink, string spotId)
        {
            foreach (FaceoffSpot spot in rink.FaceoffSpots)
            {
                if (string.Equals(spot.Id, spotId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
