using HockeyCoach.AI;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Match;

/// <summary>D-077: a coach's draws come from its own stream and never move the simulation stream.</summary>
public class CoachStreamTests
{
    /// <summary>Decides like <see cref="RotationCoach"/> but draws a varying amount of numbers from its own stream first.</summary>
    private sealed class DrawingCoach : ICoach
    {
        private readonly RotationCoach _inner;
        private readonly IRandom _random;

        public DrawingCoach(RotationCoach inner, IRandom random)
        {
            _inner = inner;
            _random = random;
        }

        public int Draws { get; private set; }

        public CoachDecision AtStoppage(CoachView view)
        {
            Burn();
            return _inner.AtStoppage(view);
        }

        public LineChoice OnTheFly(CoachView view)
        {
            Burn();
            return _inner.OnTheFly(view);
        }

        private void Burn()
        {
            int count = 1 + _random.NextInt(5);
            for (int i = 0; i < count; i++)
            {
                _random.NextDouble();
            }

            Draws += count;
        }
    }

    private static string Log(MatchResult result, ulong seed) => MatchJson.Write(result, MatchTestData.Setup, seed);

    [Theory]
    [InlineData(1UL)]
    [InlineData(42UL)]
    public void CoachDraws_DoNotChangeTheSimulationLog(ulong seed)
    {
        var data = ShiftTestData.Data;
        var streams = new MatchStreams(seed);
        var home = new DrawingCoach(new RotationCoach(data.Plays, HarnessTeams.System(data, "forecheck212")), streams.HomeCoach());
        var away = new DrawingCoach(new RotationCoach(data.Plays, HarnessTeams.System(data, "trap122")), streams.AwayCoach());

        MatchResult drawing = MatchSimulator.Run(MatchTestData.Setup, home, away, streams.Simulation());
        MatchResult plain = MatchCommand.Play(data, MatchTestData.Setup, "forecheck212", "trap122", seed);

        Assert.True(home.Draws > 0 && away.Draws > 0);
        Assert.Equal(Log(plain, seed), Log(drawing, seed));
    }

    [Fact]
    public void MatchCommand_UsesTheSeedOnlySimulationStream_SoExistingLogsAreUnchanged()
    {
        var data = ShiftTestData.Data;
        MatchResult viaStreams = MatchCommand.Play(data, MatchTestData.Setup, "forecheck212", "trap122", 42UL);
        MatchResult viaSeed = MatchSimulator.Run(
            MatchTestData.Setup,
            new RotationCoach(data.Plays, HarnessTeams.System(data, "forecheck212")),
            new RotationCoach(data.Plays, HarnessTeams.System(data, "trap122")),
            new Pcg32(42UL));

        Assert.Equal(Log(viaSeed, 42UL), Log(viaStreams, 42UL));
    }
}
