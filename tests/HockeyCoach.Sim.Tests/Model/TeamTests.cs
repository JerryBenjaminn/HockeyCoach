using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Model;

public class TeamTests
{
    [Fact]
    public void Team_KeepsRosterAndLineOrder()
    {
        Team team = TestPlayers.Team("Home", 1);

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, team.Skaters.Select(s => s.Id));
        Assert.Equal(6, team.Goalies.Single().Id);
        Assert.Equal(team.Skaters, team.Lines.Single().Skaters);
    }

    [Fact]
    public void Line_Throws_WhenSkaterAppearsTwice()
    {
        Skater a = TestPlayers.Skater(1);

        Assert.Throws<ArgumentException>(() => new Line("L1", new[] { a, a }));
    }

    [Fact]
    public void Line_Throws_WhenEmpty()
    {
        Assert.Throws<ArgumentException>(() => new Line("L1", Array.Empty<Skater>()));
    }

    [Fact]
    public void Team_Throws_WhenPlayerIdsCollide()
    {
        var skaters = new[] { TestPlayers.Skater(1), TestPlayers.Skater(2) };
        var goalies = new[] { TestPlayers.Goalie(2) };

        Assert.Throws<ArgumentException>(() => new Team("T", skaters, goalies, Array.Empty<Line>()));
    }

    [Fact]
    public void Team_Throws_WhenLineMemberIsNotOnRoster()
    {
        var skaters = new[] { TestPlayers.Skater(1) };
        var line = new Line("L1", new[] { TestPlayers.Skater(99) });

        Assert.Throws<ArgumentException>(() => new Team("T", skaters, Array.Empty<Goalie>(), new[] { line }));
    }
}
