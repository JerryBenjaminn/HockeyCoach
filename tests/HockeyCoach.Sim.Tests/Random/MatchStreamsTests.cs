using HockeyCoach.Sim.Random;

namespace HockeyCoach.Sim.Tests.Random;

/// <summary>Derived per-match streams (D-077, tech-spec.md Determinismi).</summary>
public class MatchStreamsTests
{
    private static uint[] Draws(IRandom random, int count) => Enumerable.Range(0, count).Select(_ => random.NextUInt()).ToArray();

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(42UL)]
    public void Simulation_IsTheSameSequenceAsTheSeedOnlyGenerator(ulong seed)
    {
        Assert.Equal(Draws(new Pcg32(seed), 1000), Draws(new MatchStreams(seed).Simulation(), 1000));
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(42UL)]
    public void Streams_AreReproducibleForTheSameSeed(ulong seed)
    {
        var a = new MatchStreams(seed);
        var b = new MatchStreams(seed);

        Assert.Equal(Draws(a.HomeCoach(), 200), Draws(b.HomeCoach(), 200));
        Assert.Equal(Draws(a.AwayCoach(), 200), Draws(b.AwayCoach(), 200));
        Assert.Equal(Draws(a.Simulation(), 200), Draws(b.Simulation(), 200));
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(42UL)]
    public void Streams_OfOneSeed_ShareNoValuesInTheirFirstDraws(ulong seed)
    {
        var streams = new MatchStreams(seed);
        uint[] simulation = Draws(streams.Simulation(), 256);
        uint[] home = Draws(streams.HomeCoach(), 256);
        uint[] away = Draws(streams.AwayCoach(), 256);

        Assert.Empty(simulation.Intersect(home));
        Assert.Empty(simulation.Intersect(away));
        Assert.Empty(home.Intersect(away));
    }

    [Fact]
    public void CoachStreams_DifferBetweenNeighbouringSeeds()
    {
        Assert.NotEqual(Draws(new MatchStreams(1UL).HomeCoach(), 16), Draws(new MatchStreams(2UL).HomeCoach(), 16));
        Assert.NotEqual(Draws(new MatchStreams(1UL).AwayCoach(), 16), Draws(new MatchStreams(2UL).HomeCoach(), 16));
    }

    [Fact]
    public void Generators_AreIndependentObjects_SoDrawingFromOneDoesNotMoveAnother()
    {
        var streams = new MatchStreams(7UL);
        IRandom coach = streams.HomeCoach();
        IRandom simulation = streams.Simulation();
        Draws(coach, 500);

        Assert.Equal(Draws(new Pcg32(7UL), 50), Draws(simulation, 50));
    }
}
