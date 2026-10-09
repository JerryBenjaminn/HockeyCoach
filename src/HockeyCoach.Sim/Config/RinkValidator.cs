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

            return errors;
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
