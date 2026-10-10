using System.Collections.Generic;
using HockeyCoach.Sim.Config;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// The tuning entries the shift simulation reads (milestone 2). The general validator accepts any check set; this
    /// one makes sure a tuning file can actually run a shift, so a missing value fails at load time, not mid-shift.
    /// </summary>
    public static class ShiftTuningRequirements
    {
        private static readonly string[][] Scalars =
        {
            new[] { "faceoff", "homeAdvantage" },
            new[] { "pass", "crossIce" },
            new[] { "pass", "underPressure" },
            new[] { "block", "distancePerNode" },
            new[] { "rebound", "shotPowerPerPoint" },
            new[] { "loosePuck", "extraPlayer" },
            new[] { "loosePuck", "distancePerNode" },
        };

        private static readonly string[][] Tables =
        {
            new[] { "pass", "laneDefenderDistance" },
            new[] { "deke", "defenderDistance" },
            new[] { "block", "laneDistance" },
        };

        private static readonly string[][] Parameters =
        {
            new[] { "pass", "interceptionShare" },
            new[] { "block", "maxLaneDistance" },
            new[] { "rebound", "controlledHoldShare" },
            new[] { "loosePuck", "noWinnerShare" },
            new[] { "loosePuck", "extraPlayerRadius" },
            new[] { "dumpIn", "goaliePuckHandlingPerPoint" },
            new[] { "dumpIn", "goalieReachNodes" },
        };

        private static readonly string[] Checks = { "faceoff", "pass", "zoneEntryCarry", "deke", "breakout", "block", "rebound", "loosePuck", "dumpIn" };

        private static readonly string[] ShotModifiers = { "royalRoad", "screen" };

        private static readonly string[] Times = { "faceoff", "skate", "pass", "shoot", "driveNet", "dumpIn", "loosePuck", "rebound", "systemStep" };

        /// <summary>Every missing entry as "path: missing (used by the shift simulation)". Empty means the tuning can run a shift.</summary>
        public static IReadOnlyList<string> Validate(TuningConfig tuning)
        {
            var errors = new List<string>();
            foreach (string id in Checks)
            {
                if (!tuning.Checks.ContainsKey(id))
                {
                    errors.Add(Missing("checks." + id));
                }
            }

            foreach (string[] entry in Scalars)
            {
                if (tuning.Checks.TryGetValue(entry[0], out CheckDefinition check) && !check.Modifiers.Scalars.ContainsKey(entry[1]))
                {
                    errors.Add(Missing("checks." + entry[0] + ".modifiers." + entry[1]));
                }
            }

            foreach (string[] entry in Tables)
            {
                if (tuning.Checks.TryGetValue(entry[0], out CheckDefinition check) && !Contains(check.Modifiers.TableNames, entry[1]))
                {
                    errors.Add(Missing("checks." + entry[0] + ".modifiers." + entry[1]));
                }
            }

            foreach (string[] entry in Parameters)
            {
                if (tuning.Checks.TryGetValue(entry[0], out CheckDefinition check) && !check.Parameters.ContainsKey(entry[1]))
                {
                    errors.Add(Missing("checks." + entry[0] + "." + entry[1]));
                }
            }

            foreach (string name in ShotModifiers)
            {
                if (!tuning.Shot.Modifiers.Scalars.ContainsKey(name))
                {
                    errors.Add(Missing("checks.shot.modifiers." + name));
                }
            }

            foreach (string key in Times)
            {
                if (!tuning.Time.SecondsPerAction.ContainsKey(key))
                {
                    errors.Add(Missing("time.secondsPerAction." + key));
                }
            }

            return errors;
        }

        private static bool Contains(IEnumerable<string> names, string name)
        {
            foreach (string candidate in names)
            {
                if (string.Equals(candidate, name, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Missing(string path)
        {
            return path + ": missing (used by the shift simulation)";
        }
    }
}
