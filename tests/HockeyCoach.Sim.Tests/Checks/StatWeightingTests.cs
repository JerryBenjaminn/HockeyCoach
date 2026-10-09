using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Checks;

public class StatWeightingTests
{
    [Fact]
    public void MultiPlayerRole_UsesMeanOfStats()
    {
        var participants = new CheckParticipants()
            .With("carrier", TestPlayers.Skater(1, 10))
            .With("forecheckers", TestPlayers.Skater(2, 8), TestPlayers.Skater(3, 15));

        double d = StatWeighting.Rating(TestChecks.Breakout().Defender, participants);

        Assert.Equal(11.5, d, 12);
    }

    [Fact]
    public void MultiPlayerRole_DoesNotGrowWithPlayerCount()
    {
        var one = new CheckParticipants().With("forecheckers", TestPlayers.Skater(2, 12));
        var three = new CheckParticipants().With("forecheckers", TestPlayers.Skater(2, 12), TestPlayers.Skater(3, 12), TestPlayers.Skater(4, 12));

        Assert.Equal(
            StatWeighting.Rating(TestChecks.Breakout().Defender, one),
            StatWeighting.Rating(TestChecks.Breakout().Defender, three),
            12);
    }

    [Fact]
    public void MultiPlayerRole_TakesMeanPerStatBeforeWeighting()
    {
        var fast = new Skater(2, "Fast", Position.LeftWing, SkaterStats.Uniform(10).With(SkaterStat.Speed, 20));
        var smart = new Skater(3, "Smart", Position.RightWing, SkaterStats.Uniform(10).With(SkaterStat.Awareness, 20));
        var participants = new CheckParticipants().With("forecheckers", fast, smart);

        double d = StatWeighting.Rating(TestChecks.Breakout().Defender, participants);

        // speed mean 15, awareness mean 15
        Assert.Equal(0.5 * 15 + 0.5 * 15, d, 12);
    }

    [Fact]
    public void CheckParticipants_Throws_WhenRoleHasNoPlayers()
    {
        Assert.Throws<ArgumentException>(() => new CheckParticipants().With("forecheckers"));
    }
}
