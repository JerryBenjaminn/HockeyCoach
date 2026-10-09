using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Validates a constructed <see cref="Rink"/> against the data rules of docs/data-schema.md (rink.json → Validointi).
    /// Grid completeness and node order are checked by the loader and the <see cref="Rink"/> constructor.
    /// Node zones are derived from the zone x-ranges (D-028), so the ranges must cover every x exactly once.
    /// </summary>
    public static class RinkValidator
    {
        /// <summary>Returns every problem found, each prefixed with its data path. Empty means valid.</summary>
        public static IReadOnlyList<string> Validate(Rink rink)
        {
            var errors = new List<string>();
            if (rink == null)
            {
                errors.Add("rink: missing");
                return errors;
            }

            ValidateZones(rink, errors);

            var xgZones = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rink.XgZones.Count; i++)
            {
                if (string.IsNullOrEmpty(rink.XgZones[i]))
                {
                    errors.Add("xgZones[" + i + "]: must not be empty");
                }
                else if (!xgZones.Add(rink.XgZones[i]))
                {
                    errors.Add("xgZones[" + i + "]: duplicate xG zone " + rink.XgZones[i]);
                }
            }

            for (int id = 0; id < rink.NodeCount; id++)
            {
                RinkNode node = rink.GetNode(id);
                if (!xgZones.Contains(node.XgZone))
                {
                    errors.Add("nodes[" + id + "].xgZone: " + node.XgZone + " is not listed in xgZones");
                }
            }

            ValidatePoint("ownGoal", rink.OwnGoal, rink, errors);
            ValidatePoint("opponentGoal", rink.OpponentGoal, rink, errors);

            var spotIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rink.FaceoffSpots.Count; i++)
            {
                FaceoffSpot spot = rink.FaceoffSpots[i];
                string path = "faceoffSpots[" + i + "]";
                if (string.IsNullOrEmpty(spot.Id))
                {
                    errors.Add(path + ".id: must not be empty");
                }
                else if (!spotIds.Add(spot.Id))
                {
                    errors.Add(path + ".id: duplicate faceoff spot id " + spot.Id);
                }

                ValidatePoint(path, spot.Point, rink, errors);
            }

            ValidateGoals(rink, errors);
            ValidateSymmetry(rink, errors);
            ValidateSpotPairs(rink, errors);
            return errors;
        }

        /// <summary>
        /// Goal nodes (D-033): on the middle lane y = (width - 1) / 2, each other's image under the 180° rotation,
        /// not slot nodes and not faceoff spots.
        /// </summary>
        private static void ValidateGoals(Rink rink, List<string> errors)
        {
            bool inside = rink.Contains(rink.OwnGoal) && rink.Contains(rink.OpponentGoal);
            foreach (KeyValuePair<string, GridPoint> goal in new[]
            {
                new KeyValuePair<string, GridPoint>("ownGoal", rink.OwnGoal),
                new KeyValuePair<string, GridPoint>("opponentGoal", rink.OpponentGoal),
            })
            {
                if (2 * goal.Value.Y != rink.Width - 1)
                {
                    errors.Add(goal.Key + ": " + goal.Value + " is not on the middle lane y = (width - 1) / 2");
                }

                if (rink.Contains(goal.Value) && rink.GetNode(goal.Value.X, goal.Value.Y).IsSlot)
                {
                    errors.Add(goal.Key + ": goal node " + goal.Value + " must have isSlot false (D-033)");
                }

                foreach (FaceoffSpot spot in rink.FaceoffSpots)
                {
                    if (spot.Point.Equals(goal.Value))
                    {
                        errors.Add(goal.Key + ": goal node " + goal.Value + " must not be a faceoff spot (" + spot.Id + ")");
                    }
                }
            }

            if (inside && !rink.Rotate(rink.OwnGoal).Equals(rink.OpponentGoal))
            {
                errors.Add("opponentGoal: " + rink.OpponentGoal + " is not the 180° rotation of ownGoal " + rink.OwnGoal);
            }
        }

        /// <summary>The rink must be symmetric across the long axis so that mirrored plays stay valid (D-034).</summary>
        private static void ValidateSymmetry(Rink rink, List<string> errors)
        {
            for (int x = 0; x < rink.Length; x++)
            {
                for (int y = 0; y < rink.Width - 1 - y; y++)
                {
                    RinkNode node = rink.GetNode(x, y);
                    RinkNode mirror = rink.GetNode(x, rink.Width - 1 - y);
                    if (!string.Equals(node.XgZone, mirror.XgZone, StringComparison.Ordinal) || node.IsSlot != mirror.IsSlot)
                    {
                        errors.Add("nodes[" + rink.IdOf(x, y) + "]: " + node + " and its mirror " + mirror
                            + " must have the same xgZone and isSlot (y-symmetry)");
                    }
                }
            }
        }

        /// <summary>
        /// Every <c>...Left</c> spot has a <c>...Right</c> partner at its mirror node and vice versa; a spot on the
        /// middle lane does not end in Left or Right.
        /// </summary>
        private static void ValidateSpotPairs(Rink rink, List<string> errors)
        {
            for (int i = 0; i < rink.FaceoffSpots.Count; i++)
            {
                FaceoffSpot spot = rink.FaceoffSpots[i];
                string path = "faceoffSpots[" + i + "]";
                bool sided = spot.Id.EndsWith("Left", StringComparison.Ordinal) || spot.Id.EndsWith("Right", StringComparison.Ordinal);
                if (!sided)
                {
                    continue;
                }

                if (2 * spot.Point.Y == rink.Width - 1)
                {
                    errors.Add(path + ".id: " + spot.Id + " is on the middle lane and must not end in Left or Right");
                    continue;
                }

                string partnerId = spot.Id.EndsWith("Left", StringComparison.Ordinal)
                    ? spot.Id.Substring(0, spot.Id.Length - 4) + "Right"
                    : spot.Id.Substring(0, spot.Id.Length - 5) + "Left";
                FaceoffSpot partner = null;
                foreach (FaceoffSpot candidate in rink.FaceoffSpots)
                {
                    if (string.Equals(candidate.Id, partnerId, StringComparison.Ordinal))
                    {
                        partner = candidate;
                    }
                }

                if (partner == null)
                {
                    errors.Add(path + ".id: " + spot.Id + " has no mirror partner " + partnerId);
                }
                else if (!partner.Point.Equals(rink.MirrorY(spot.Point)))
                {
                    errors.Add(path + ": " + spot.Id + " " + spot.Point + " and " + partnerId + " " + partner.Point + " are not mirror images");
                }
            }
        }

        private static void ValidateZones(Rink rink, List<string> errors)
        {
            var owners = new int[rink.Length];
            var seen = new HashSet<RinkZone>();
            for (int i = 0; i < rink.Zones.Count; i++)
            {
                ZoneRange zone = rink.Zones[i];
                string path = "zones[" + i + "]";
                if (!seen.Add(zone.Zone))
                {
                    errors.Add(path + ".id: zone listed twice");
                }

                if (zone.XMin > zone.XMax)
                {
                    errors.Add(path + ": xMin (" + zone.XMin + ") is greater than xMax (" + zone.XMax + ")");
                }

                for (int x = zone.XMin; x <= zone.XMax; x++)
                {
                    if (x < 0 || x >= rink.Length)
                    {
                        errors.Add(path + ": x " + x + " is outside the rink (length " + rink.Length + ")");
                        continue;
                    }

                    owners[x]++;
                }
            }

            for (int x = 0; x < rink.Length; x++)
            {
                if (owners[x] != 1)
                {
                    errors.Add("zones: x " + x + " belongs to " + owners[x] + " zones, expected exactly 1");
                }
            }
        }

        private static void ValidatePoint(string path, GridPoint point, Rink rink, List<string> errors)
        {
            if (!rink.Contains(point.X, point.Y))
            {
                errors.Add(path + ": " + point + " is outside the " + rink.Length + "x" + rink.Width + " grid");
            }
        }
    }
}
