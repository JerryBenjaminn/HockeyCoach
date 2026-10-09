using System;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Participant role names allowed in check definitions (docs/data-schema.md, Osallistujaroolit).
    /// A role is not a position.
    /// </summary>
    public static class CheckRoles
    {
        /// <summary>The goalie role; it reads goalie stats, every other role reads skater stats.</summary>
        public const string Goalie = "goalie";

        private static readonly string[] Known =
        {
            "carrier", "centre", "forecheckers", "goalie", "hitter", "nearestDefender", "participant", "passer", "receiver", "shooter",
        };

        /// <summary>All known role names, sorted ordinally.</summary>
        public static string[] All()
        {
            return (string[])Known.Clone();
        }

        /// <summary>Whether the role name is known (ordinal).</summary>
        public static bool IsKnown(string role)
        {
            return Array.IndexOf(Known, role) >= 0;
        }

        /// <summary>Which stat set the role reads.</summary>
        public static StatOwner StatOwnerOf(string role)
        {
            return string.Equals(role, Goalie, StringComparison.Ordinal) ? StatOwner.Goalie : StatOwner.Skater;
        }
    }
}
