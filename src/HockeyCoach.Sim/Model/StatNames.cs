using System;

namespace HockeyCoach.Sim.Model
{
    /// <summary>
    /// Maps stats to and from the camelCase names used in the data files. Lookup is ordinal (case-sensitive).
    /// </summary>
    public static class StatNames
    {
        private static readonly string[] SkaterNames =
        {
            "speed", "agility", "endurance", "hands", "passing", "shotAccuracy",
            "shotPower", "positioning", "awareness", "strength", "discipline", "faceoffs",
        };

        private static readonly string[] GoalieNames =
        {
            "reflexes", "positioning", "mobility", "reboundControl", "puckHandling", "mentalToughness",
        };

        /// <summary>Number of skater stats.</summary>
        public static int SkaterStatCount
        {
            get { return SkaterNames.Length; }
        }

        /// <summary>Number of goalie stats.</summary>
        public static int GoalieStatCount
        {
            get { return GoalieNames.Length; }
        }

        /// <summary>Returns the data name of a skater stat.</summary>
        public static string ToName(SkaterStat stat)
        {
            return SkaterNames[(int)stat];
        }

        /// <summary>Returns the data name of a goalie stat.</summary>
        public static string ToName(GoalieStat stat)
        {
            return GoalieNames[(int)stat];
        }

        /// <summary>Parses a skater stat data name, e.g. <c>shotAccuracy</c>.</summary>
        public static bool TryParseSkater(string name, out SkaterStat stat)
        {
            int index = IndexOf(SkaterNames, name);
            stat = index >= 0 ? (SkaterStat)index : default;
            return index >= 0;
        }

        /// <summary>Parses a goalie stat data name, e.g. <c>reboundControl</c>.</summary>
        public static bool TryParseGoalie(string name, out GoalieStat stat)
        {
            int index = IndexOf(GoalieNames, name);
            stat = index >= 0 ? (GoalieStat)index : default;
            return index >= 0;
        }

        /// <summary>Parses a stat name within the given stat set.</summary>
        public static bool TryParse(StatOwner owner, string name, out StatRef stat)
        {
            if (owner == StatOwner.Skater && TryParseSkater(name, out SkaterStat skaterStat))
            {
                stat = StatRef.Of(skaterStat);
                return true;
            }

            if (owner == StatOwner.Goalie && TryParseGoalie(name, out GoalieStat goalieStat))
            {
                stat = StatRef.Of(goalieStat);
                return true;
            }

            stat = default;
            return false;
        }

        private static int IndexOf(string[] names, string name)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(names[i], name, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
