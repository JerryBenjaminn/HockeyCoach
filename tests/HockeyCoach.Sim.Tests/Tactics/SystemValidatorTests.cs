using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Tactics;

public class SystemValidatorTests
{
    private static readonly Rink Rink = TestRinks.Standard();

    private static KeyValuePair<SystemRole, SystemTarget>[] Fixed() => TestPlays.Targets(
        SystemTarget.PuckOffset(0, 0),
        SystemTarget.Node(new GridPoint(3, 1)),
        SystemTarget.Node(new GridPoint(3, 3)),
        SystemTarget.Node(new GridPoint(2, 1)),
        SystemTarget.Node(new GridPoint(2, 3)));

    private static IReadOnlyList<string> Validate(bool mirrorY, params SystemRule[] rules)
    {
        return SystemValidator.Validate(new DefensiveSystem("test", "Test", mirrorY, rules), Rink);
    }

    [Fact]
    public void SchemaExample_IsValid()
    {
        Assert.Empty(SystemValidator.Validate(TestPlays.Trap122(), Rink));
    }

    [Fact]
    public void EmptyRules_AreRejected()
    {
        Assert.Contains("rules: must not be empty", Validate(true));
    }

    [Fact]
    public void LastRule_MustBeTheEmptyFallback()
    {
        var conditional = new SystemRule(new SystemCondition(new[] { RinkZone.Offensive }, null, null, null), Fixed());

        Assert.Contains(Validate(true, conditional), e => e.StartsWith("rules[0].when: the last rule is the fallback", StringComparison.Ordinal));
    }

    [Fact]
    public void EmptyConditionBeforeTheLastRule_IsRejected()
    {
        var always = new SystemRule(SystemCondition.Always, Fixed());

        Assert.Contains(Validate(true, always, always), e => e.StartsWith("rules[0].when: only the last rule may be empty", StringComparison.Ordinal));
    }

    [Fact]
    public void BadConditions_AreRejected()
    {
        var emptyZones = new SystemRule(new SystemCondition(Array.Empty<RinkZone>(), null, null, null), Fixed());
        var reversed = new SystemRule(new SystemCondition(null, new IntRange(5, 3), null, null), Fixed());
        var outside = new SystemRule(new SystemCondition(null, new IntRange(0, 11), null, null), Fixed());
        var fallback = new SystemRule(SystemCondition.Always, Fixed());

        IReadOnlyList<string> errors = Validate(false, emptyZones, reversed, outside, fallback);

        Assert.Contains("rules[0].when.puckZones: must not be empty", errors);
        Assert.Contains("rules[1].when.puckX: min (5) is greater than max (3)", errors);
        Assert.Contains(errors, e => e.StartsWith("rules[2].when.puckX: [0, 11] is outside the grid", StringComparison.Ordinal));
    }

    [Fact]
    public void MirrorYSystem_RejectsPuckYReachingTheRightSide()
    {
        var right = new SystemRule(new SystemCondition(null, null, new IntRange(2, 3), null), Fixed());
        var fallback = new SystemRule(SystemCondition.Always, Fixed());

        Assert.Contains(Validate(true, right, fallback), e => e.StartsWith("rules[0].when.puckY: a mirrorY system", StringComparison.Ordinal));
        Assert.Empty(Validate(false, right, fallback));
    }

    [Fact]
    public void MissingRole_IsRejected()
    {
        var rule = new SystemRule(SystemCondition.Always, Fixed().Where(t => t.Key != SystemRole.D2));

        Assert.Contains(Validate(true, rule), e => e.StartsWith("rules[0].targets.D2: missing", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(1, 2, "is a goal node")]
    [InlineData(9, 2, "is a goal node")]
    [InlineData(11, 0, "is outside")]
    public void TargetNode_MustBeOnTheGridAndNotAGoal(int x, int y, string message)
    {
        var targets = Fixed().Select(t => t.Key == SystemRole.D1 ? new KeyValuePair<SystemRole, SystemTarget>(SystemRole.D1, SystemTarget.Node(new GridPoint(x, y))) : t);

        Assert.Contains(Validate(true, new SystemRule(SystemCondition.Always, targets)), e => e.StartsWith("rules[0].targets.D1.node: (" + x + "," + y + ") " + message, StringComparison.Ordinal));
    }
}
