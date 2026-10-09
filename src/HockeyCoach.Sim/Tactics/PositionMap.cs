using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>Copies position → node pairs into a dictionary ordered by position (C, LW, RW, LD, RD).</summary>
    internal static class PositionMap
    {
        internal static SortedDictionary<Position, GridPoint> Copy(IEnumerable<KeyValuePair<Position, GridPoint>> entries, string paramName)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(paramName);
            }

            var map = new SortedDictionary<Position, GridPoint>();
            foreach (KeyValuePair<Position, GridPoint> entry in entries)
            {
                if (map.ContainsKey(entry.Key))
                {
                    throw new ArgumentException("Position " + Positions.ToName(entry.Key) + " is listed twice.", paramName);
                }

                map.Add(entry.Key, entry.Value);
            }

            return map;
        }
    }
}
