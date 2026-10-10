using HockeyCoach.Harness.Data;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Shift;

/// <summary>Shared repository data and helpers for shift tests.</summary>
internal static class ShiftTestData
{
    public static readonly GameData Data = GameData.Load(Path.Combine(Fixtures.RepoRoot(), "data"));

    public static ShiftSetup Setup(int maxSteps = 2000, string home = "forecheck212", string away = "trap122")
    {
        return HarnessTeams.Shift(Data, home, away, maxSteps);
    }

    public static ShiftResult Run(ulong seed, ShiftSetup? setup = null)
    {
        return ShiftSimulator.Run(setup ?? Setup(), new Pcg32(seed));
    }

    public static IEnumerable<T> Of<T>(ShiftResult result)
        where T : SimEvent => result.Log.Events.OfType<T>();
}
