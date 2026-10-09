using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Model;

public class TeamTests
{
    [Fact]
    public void Team_KeepsTriosAndPairsAsSeparateUnits()
    {
        Team team = TestPlayers.Team("Home", 1, trios: 4, pairs: 3);

        Assert.Equal(4, team.ForwardLines.Count);
        Assert.Equal(3, team.DefencePairs.Count);
        Assert.Equal(4 * ForwardLine.Size + 3 * DefencePair.Size, team.Skaters.Count);
        Assert.All(team.ForwardLines, l => Assert.Equal(ForwardLine.Size, l.Skaters.Count));
        Assert.All(team.DefencePairs, p => Assert.Equal(DefencePair.Size, p.Skaters.Count));
    }

    [Fact]
    public void ForwardLine_ListsSkatersInSlotOrderLwCRw()
    {
        Skater lw = TestPlayers.Skater(1, position: Position.LeftWing);
        Skater c = TestPlayers.Skater(2, position: Position.Center);
        Skater rw = TestPlayers.Skater(3, position: Position.RightWing);

        var line = new ForwardLine("F1", lw, c, rw);

        Assert.Equal(new[] { lw, c, rw }, line.Skaters);
        Assert.Equal(Position.Center, line.SlotOf(c));
        Assert.Null(line.SlotOf(TestPlayers.Skater(4)));
    }

    [Fact]
    public void ForwardLine_Throws_WhenSkaterAppearsTwice()
    {
        Skater a = TestPlayers.Skater(1);

        Assert.Throws<ArgumentException>(() => new ForwardLine("F1", a, TestPlayers.Skater(2), a));
    }

    [Fact]
    public void DefencePair_Throws_WhenSkaterAppearsTwice()
    {
        Skater a = TestPlayers.Skater(1, position: Position.LeftDefence);

        Assert.Throws<ArgumentException>(() => new DefencePair("D1", a, a));
    }

    [Fact]
    public void OnIceSkaters_HasFiveDistinctSkaters_FromAnyTrioAndPair()
    {
        Team team = TestPlayers.Team("Home", 1, trios: 2, pairs: 2);

        foreach (ForwardLine trio in team.ForwardLines)
        {
            foreach (DefencePair pair in team.DefencePairs)
            {
                var onIce = new OnIceSkaters(trio, pair);
                Assert.Equal(OnIceSkaters.Count, onIce.Skaters.Count);
                Assert.Equal(5, onIce.Skaters.Select(s => s.Id).Distinct().Count());
            }
        }
    }

    [Fact]
    public void OnIceSkaters_Throws_WhenTrioAndPairSharePlayer()
    {
        Skater shared = TestPlayers.Skater(1);
        var trio = new ForwardLine("F1", shared, TestPlayers.Skater(2), TestPlayers.Skater(3));
        var pair = new DefencePair("D1", TestPlayers.Skater(4), shared);

        Assert.Throws<ArgumentException>(() => new OnIceSkaters(trio, pair));
    }

    [Fact]
    public void OnIceSkaters_DetectsOffSidePlayer()
    {
        Skater leftWingPlayingRight = TestPlayers.Skater(3, position: Position.LeftWing);
        var trio = new ForwardLine("F1", TestPlayers.Skater(1, position: Position.LeftWing), TestPlayers.Skater(2, position: Position.Center), leftWingPlayingRight);
        var pair = new DefencePair("D1", TestPlayers.Skater(4, position: Position.LeftDefence), TestPlayers.Skater(5, position: Position.RightDefence));
        var onIce = new OnIceSkaters(trio, pair);

        Assert.True(onIce.IsOffSide(leftWingPlayingRight));
        Assert.False(onIce.IsOffSide(trio.LeftWing));
        Assert.False(onIce.IsOffSide(pair.RightDefence));
    }

    [Fact]
    public void Team_Throws_WhenPlayerIdsCollide()
    {
        var skaters = new[] { TestPlayers.Skater(1), TestPlayers.Skater(2) };
        var goalies = new[] { TestPlayers.Goalie(2) };

        Assert.Throws<ArgumentException>(() => new Team("T", skaters, goalies, Array.Empty<ForwardLine>(), Array.Empty<DefencePair>()));
    }

    [Fact]
    public void Team_Throws_WhenUnitMemberIsNotOnRoster()
    {
        var skaters = new[] { TestPlayers.Skater(1), TestPlayers.Skater(2) };
        var pair = new DefencePair("D1", skaters[0], TestPlayers.Skater(99));

        Assert.Throws<ArgumentException>(() => new Team("T", skaters, Array.Empty<Goalie>(), Array.Empty<ForwardLine>(), new[] { pair }));
    }
}
