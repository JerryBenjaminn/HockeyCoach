using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Tactics;

public class PlaySelectorTests
{
    private static readonly Rink Rink = TestRinks.Standard();

    private static Play Typed(string id, PlayType type, Position carrier, string? spot = null)
    {
        return TestPlays.Build(
            carrier,
            new[]
            {
                TestPlays.At(Position.LeftWing, 8, 0), TestPlays.At(Position.Center, 8, 1), TestPlays.At(Position.RightWing, 8, 4),
                TestPlays.At(Position.LeftDefence, 7, 1), TestPlays.At(Position.RightDefence, 7, 3),
            },
            new[] { new Beat(TestPlays.NoMoves, PlayAction.Shoot(carrier)) },
            type,
            spot,
            id);
    }

    /// <summary>A puck in the middle lane: no play is mirrored (D-037).</summary>
    private static readonly GridPoint MiddlePuck = new(8, 2);

    /// <summary>A puck in the lane of y = 4, the other side than a play written from y = 0.</summary>
    private static readonly GridPoint FarPuck = new(8, 4);

    private static ShiftPlan Plan(params Play[] plays) => new(plays, TestPlays.Trap122());

    [Theory]
    [InlineData(RinkZone.Defensive, PlayType.Breakout)]
    [InlineData(RinkZone.Neutral, PlayType.ZoneEntry)]
    [InlineData(RinkZone.Offensive, PlayType.OffensiveZone)]
    public void TypeFor_MapsZonesToPlayTypes(RinkZone zone, PlayType expected)
    {
        Assert.Equal(expected, PlaySelector.TypeFor(zone));
    }

    [Fact]
    public void Select_PrefersAFittingPlayStartedByTheHolder()
    {
        ShiftPlan plan = Plan(Typed("a", PlayType.OffensiveZone, Position.LeftWing), Typed("b", PlayType.OffensiveZone, Position.Center));

        Play play = PlaySelector.Select(plan, RinkZone.Offensive, Position.Center, MiddlePuck, Rink, out bool needsPass);

        Assert.Equal("b", play.Id);
        Assert.False(needsPass);
    }

    [Fact]
    public void Select_FallsBackToTheFirstFittingPlayWithAPass()
    {
        ShiftPlan plan = Plan(Typed("x", PlayType.Breakout, Position.Center), Typed("a", PlayType.OffensiveZone, Position.LeftWing), Typed("b", PlayType.OffensiveZone, Position.RightWing));

        Play play = PlaySelector.Select(plan, RinkZone.Offensive, Position.Center, MiddlePuck, Rink, out bool needsPass);

        Assert.Equal("a", play.Id);
        Assert.True(needsPass);
    }

    [Fact]
    public void Select_ComparesTheHolderToTheMirroredPuckCarrier_Q075()
    {
        // Written for LW (start (8, 0)); with the puck on the far side it runs mirrored and RW carries the puck.
        ShiftPlan plan = Plan(Typed("a", PlayType.OffensiveZone, Position.LeftWing));

        Play forRw = PlaySelector.Select(plan, RinkZone.Offensive, Position.RightWing, FarPuck, Rink, out bool rwNeedsPass);
        Assert.Equal(Position.RightWing, forRw.PuckCarrier);
        Assert.False(rwNeedsPass);

        Play forLw = PlaySelector.Select(plan, RinkZone.Offensive, Position.LeftWing, FarPuck, Rink, out bool lwNeedsPass);
        Assert.Equal(Position.RightWing, forLw.PuckCarrier);
        Assert.True(lwNeedsPass);
    }

    [Fact]
    public void Select_PrefersALaterPlayWhoseMirroredCarrierIsTheHolder()
    {
        // "c" is carried by C either way; "a" (written for LW) runs mirrored as RW's play, so the RW holder gets "a".
        ShiftPlan plan = Plan(Typed("c", PlayType.OffensiveZone, Position.Center), Typed("a", PlayType.OffensiveZone, Position.LeftWing));

        Play play = PlaySelector.Select(plan, RinkZone.Offensive, Position.RightWing, FarPuck, Rink, out bool needsPass);

        Assert.Equal("a", play.Id);
        Assert.False(needsPass);
    }

    [Fact]
    public void Select_RunsTheWrittenPlay_WhenThePuckIsOnItsSide()
    {
        ShiftPlan plan = Plan(Typed("a", PlayType.OffensiveZone, Position.LeftWing));

        Play play = PlaySelector.Select(plan, RinkZone.Offensive, Position.LeftWing, new GridPoint(8, 0), Rink, out bool needsPass);

        Assert.Equal(Position.LeftWing, play.PuckCarrier);
        Assert.False(needsPass);
    }

    [Fact]
    public void Select_ReturnsNull_WhenNoPlayFitsTheZone()
    {
        ShiftPlan plan = Plan(Typed("x", PlayType.Breakout, Position.Center), Typed("f", PlayType.Faceoff, Position.Center, "offensiveLeft"));

        Assert.Null(PlaySelector.Select(plan, RinkZone.Offensive, Position.Center, MiddlePuck, Rink, out bool needsPass));
        Assert.False(needsPass);
    }

    [Fact]
    public void FaceoffPlay_IsUsedAtItsSpotAndItsMirror()
    {
        ShiftPlan plan = Plan(Typed("f", PlayType.Faceoff, Position.Center, "offensiveLeft"));

        Assert.NotNull(PlaySelector.FaceoffPlay(plan, "offensiveLeft", out bool own));
        Assert.False(own);
        Assert.NotNull(PlaySelector.FaceoffPlay(plan, "offensiveRight", out bool mirrored));
        Assert.True(mirrored);
        Assert.Null(PlaySelector.FaceoffPlay(plan, "center", out _));
    }

    [Theory]
    [InlineData(5, 2)]
    [InlineData(8, 1)]
    [InlineData(2, 3)]
    public void DefaultFaceoffFormation_IsValidForTheSpot(int x, int y)
    {
        IReadOnlyDictionary<Position, GridPoint> nodes = FaceoffFormation.Default(new GridPoint(x, y), Rink);

        Assert.Equal(5, nodes.Count);
        Assert.Equal(new GridPoint(x, y), nodes[Position.Center]);
        Assert.Equal(5, nodes.Values.Distinct().Count());
        Assert.All(nodes.Values, n => Assert.True(Rink.Contains(n) && !Rink.IsGoalNode(n)));
    }
}
