using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Tactics;

public class SystemModelTests
{
    private static readonly Rink Rink = TestRinks.Standard();

    [Fact]
    public void EmptyCondition_AlwaysMatches()
    {
        Assert.True(SystemCondition.Always.IsEmpty);
        Assert.True(SystemCondition.Always.Matches(new GridPoint(0, 0), RinkZone.Defensive, PuckState.Loose));
    }

    [Fact]
    public void Condition_RequiresEveryGivenPart()
    {
        var when = new SystemCondition(new[] { RinkZone.Offensive }, new IntRange(7, 8), new IntRange(0, 2), PuckState.Controlled);

        Assert.False(when.IsEmpty);
        Assert.True(when.Matches(new GridPoint(8, 1), RinkZone.Offensive, PuckState.Controlled));
        Assert.False(when.Matches(new GridPoint(8, 1), RinkZone.Neutral, PuckState.Controlled));
        Assert.False(when.Matches(new GridPoint(9, 1), RinkZone.Offensive, PuckState.Controlled));
        Assert.False(when.Matches(new GridPoint(8, 3), RinkZone.Offensive, PuckState.Controlled));
        Assert.False(when.Matches(new GridPoint(8, 1), RinkZone.Offensive, PuckState.Loose));
    }

    [Fact]
    public void Target_MirrorsNodeAndNegatesOffsetDy()
    {
        SystemTarget node = SystemTarget.Node(new GridPoint(6, 1)).Mirror(Rink);
        SystemTarget offset = SystemTarget.PuckOffset(-1, 1).Mirror(Rink);

        Assert.False(node.IsPuckOffset);
        Assert.Equal(new GridPoint(6, 3), node.Value);
        Assert.True(offset.IsPuckOffset);
        Assert.Equal(new GridPoint(-1, -1), offset.Value);
    }

    [Fact]
    public void Rule_KeepsTargetsInRoleOrder()
    {
        DefensiveSystem trap = TestPlays.Trap122();

        Assert.Equal(SystemNames.AllRoles, trap.Rules[0].Targets.Keys);
        Assert.True(trap.Rules[1].When.IsEmpty);
    }

    [Fact]
    public void Rule_RejectsDuplicateRole()
    {
        var targets = new[]
        {
            new KeyValuePair<SystemRole, SystemTarget>(SystemRole.F1, SystemTarget.PuckOffset(0, 0)),
            new KeyValuePair<SystemRole, SystemTarget>(SystemRole.F1, SystemTarget.PuckOffset(0, 0)),
        };

        Assert.Throws<ArgumentException>(() => new SystemRule(SystemCondition.Always, targets));
    }
}
