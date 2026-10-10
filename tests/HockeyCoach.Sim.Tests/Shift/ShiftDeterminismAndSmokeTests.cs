using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Shift;

namespace HockeyCoach.Sim.Tests.Shift;

public class ShiftDeterminismAndSmokeTests
{
    /// <summary>The full log as JSON (every field and placement), for exact comparisons.</summary>
    private static string Serialize(ShiftResult result) => EventJson.Write(result, 0UL);

    [Theory]
    [InlineData(42UL)]
    [InlineData(7UL)]
    [InlineData(123456789UL)]
    public void SameSeed_ProducesIdenticalEventLogs(ulong seed)
    {
        string first = Serialize(ShiftTestData.Run(seed));
        string second = Serialize(ShiftTestData.Run(seed));

        Assert.Equal(first, second);
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentEventLogs()
    {
        Assert.NotEqual(Serialize(ShiftTestData.Run(42UL)), Serialize(ShiftTestData.Run(43UL)));
    }

    [Theory]
    [InlineData("forecheck212", "trap122")]
    [InlineData("trap122", "forecheck212")]
    [InlineData("trap122", "trap122")]
    [InlineData("forecheck212", "forecheck212")]
    public void Smoke_1000Shifts_WithoutExceptionsOrStalls(string home, string away)
    {
        ShiftSetup setup = ShiftTestData.Setup(home: home, away: away);
        int stalled = 0;
        for (ulong seed = 1; seed <= 1000; seed++)
        {
            ShiftResult result = ShiftTestData.Run(seed, setup);
            if (result.EndReason == ShiftEndReason.StepCap)
            {
                stalled++;
            }
        }

        Assert.Equal(0, stalled);
    }
}
