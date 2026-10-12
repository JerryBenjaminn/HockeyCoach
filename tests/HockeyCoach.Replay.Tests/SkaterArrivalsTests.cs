using HockeyCoach.Replay.Core;

namespace HockeyCoach.Replay.Tests;

/// <summary>Precomputed skater target and arrival times (data-schema.md, replay.json, Luistelijat).</summary>
public sealed class SkaterArrivalsTests
{
    [Fact]
    public void Compute_ArrivesOnTime_WhenTheLegFitsAtTopSpeed()
    {
        double[] a = SkaterArrivals.Compute(new[] { 0.0, 2.0, 4.0 }, new[] { 0.0, 10.0, 10.0 }, 10, null);
        Assert.Equal(new[] { 0.0, 2.0, 4.0 }, a);
    }

    [Fact]
    public void Compute_ArrivesLate_AndTheLatenessCarriesOver()
    {
        // Leg 1 needs 3 s at top speed but has 1 s; leg 2 starts from the late arrival.
        double[] a = SkaterArrivals.Compute(new[] { 0.0, 1.0, 2.0, 10.0 }, new[] { 0.0, 30.0, 10.0, 10.0 }, 10, null);
        Assert.Equal(new[] { 0.0, 3.0, 4.0, 10.0 }, a);
    }

    [Fact]
    public void Compute_ResetsTheLateness_AtAnApproach()
    {
        double[] a = SkaterArrivals.Compute(new[] { 0.0, 1.0, 1.5 }, new[] { 0.0, 30.0, 30.0 }, 10, new[] { false, false, true });
        Assert.Equal(new[] { 0.0, 3.0, 1.5 }, a);
    }

    [Fact]
    public void Compute_RejectsMismatchedInputs()
    {
        Assert.Throws<ArgumentException>(() => SkaterArrivals.Compute(new[] { 0.0, 1.0 }, new[] { 0.0 }, 10, null));
        Assert.Throws<ArgumentException>(() => SkaterArrivals.Compute(new[] { 0.0 }, new[] { 0.0 }, 10, new[] { true, false }));
        Assert.Throws<ArgumentOutOfRangeException>(() => SkaterArrivals.Compute(new[] { 0.0 }, new[] { 0.0 }, 0, null));
    }
}
