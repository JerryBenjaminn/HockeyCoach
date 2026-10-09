using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Model
{
    /// <summary>Shared guard for line units.</summary>
    internal static class Units
    {
        internal static void RequireDistinct(IReadOnlyList<Skater> skaters, string unitName)
        {
            for (int i = 0; i < skaters.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (skaters[i].Id == skaters[j].Id)
                    {
                        throw new ArgumentException("Skater " + skaters[i] + " appears twice in " + unitName + ".");
                    }
                }
            }
        }
    }
}
