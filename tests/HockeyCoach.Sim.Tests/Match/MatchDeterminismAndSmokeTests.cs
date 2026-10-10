using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Match;

/// <summary>Full-match determinism and smoke tests (tech-spec.md; Q-I: the 1000-match run has its own category).</summary>
public class MatchDeterminismAndSmokeTests
{
    private static string Serialize(MatchResult result, ulong seed) => MatchJson.Write(result, MatchTestData.Setup, seed);

    private static MatchResult Fresh(ulong seed, string home = "forecheck212", string away = "trap122") =>
        MatchCommand.Play(ShiftTestData.Data, MatchTestData.Setup, home, away, seed);

    [Theory]
    [InlineData(42UL)]
    [InlineData(7UL)]
    public void SameSeed_ProducesIdenticalMatchLogs(ulong seed)
    {
        Assert.Equal(Serialize(Fresh(seed), seed), Serialize(Fresh(seed), seed));
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentMatchLogs()
    {
        Assert.NotEqual(Serialize(Fresh(42UL), 0UL), Serialize(Fresh(43UL), 0UL));
    }

    [Theory]
    [InlineData("forecheck212", "trap122")]
    [InlineData("trap122", "forecheck212")]
    public void Smoke_ShortRun_WithoutExceptionsOrStalls(string home, string away)
    {
        for (ulong seed = 1; seed <= 25; seed++)
        {
            Assert.False(Fresh(seed, home, away).Stalled, "stalled at seed " + seed);
        }
    }

    /// <summary>The 1000-match smoke test (Q-I). Run with: dotnet test -s tests/HockeyCoach.Sim.Tests/smoke.runsettings</summary>
    [Fact]
    [Trait("Category", "Smoke")]
    public void Smoke_1000Matches_WithoutExceptionsOrStalls()
    {
        string[] systems = { "forecheck212", "trap122" };
        int stalled = 0;
        for (ulong seed = 1; seed <= 1000; seed++)
        {
            string home = systems[(int)(seed % 2)];
            string away = systems[(int)((seed / 2) % 2)];
            stalled += Fresh(seed, home, away).Stalled ? 1 : 0;
        }

        Assert.Equal(0, stalled);
    }
}
