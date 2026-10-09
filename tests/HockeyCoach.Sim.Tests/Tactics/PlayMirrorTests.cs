using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Tactics;

public class PlayMirrorTests
{
    private static readonly Rink Rink = TestRinks.Standard();

    [Theory]
    [InlineData(Position.LeftWing, Position.RightWing)]
    [InlineData(Position.RightWing, Position.LeftWing)]
    [InlineData(Position.LeftDefence, Position.RightDefence)]
    [InlineData(Position.RightDefence, Position.LeftDefence)]
    [InlineData(Position.Center, Position.Center)]
    public void MirrorPosition_SwapsSides_AndKeepsCentre(Position position, Position expected)
    {
        Assert.Equal(expected, PlayMirror.MirrorPosition(position));
    }

    [Theory]
    [InlineData("offensiveLeft", "offensiveRight")]
    [InlineData("neutralDefensiveRight", "neutralDefensiveLeft")]
    [InlineData("center", "center")]
    public void MirrorFaceoffSpotId_SwapsLeftAndRight(string spot, string expected)
    {
        Assert.Equal(expected, PlayMirror.MirrorFaceoffSpotId(spot));
    }

    [Fact]
    public void Mirror_FlipsNodesAndSwapsPositions()
    {
        Play mirrored = PlayMirror.Mirror(TestPlays.PointShotScreen(), Rink);

        Assert.Equal(Position.RightWing, mirrored.PuckCarrier);
        Assert.Equal(new GridPoint(8, 4), mirrored.StartPositions[Position.RightWing]);
        Assert.Equal(new GridPoint(8, 1), mirrored.StartPositions[Position.Center]);
        Assert.Equal(new GridPoint(9, 0), mirrored.StartPositions[Position.LeftWing]);
        Assert.Equal(new GridPoint(7, 3), mirrored.StartPositions[Position.RightDefence]);
        Assert.Equal(new GridPoint(9, 1), mirrored.Beats[0].Moves[Position.Center]);
        Assert.Equal(Position.RightWing, mirrored.Beats[0].Action.Actor);
        Assert.Equal(Position.RightDefence, mirrored.Beats[0].Action.Receiver);
        Assert.Equal(new GridPoint(9, 3), mirrored.Beats[1].Moves[Position.RightWing]);
        Assert.Equal(Position.LeftWing, mirrored.Beats[1].Action.Actor);
        Assert.Equal(PlayActionType.Shoot, mirrored.Beats[2].Action.Type);
        Assert.Equal(Position.RightDefence, mirrored.Beats[2].Action.Actor);
        Assert.Equal("pointShotScreen", mirrored.Id);
    }

    [Fact]
    public void Mirror_MapsSkateAndDumpTargets_AndFaceoffSpot()
    {
        Play play = TestPlays.Build(
            Position.Center,
            new[]
            {
                TestPlays.At(Position.LeftWing, 8, 0), TestPlays.At(Position.Center, 8, 1), TestPlays.At(Position.RightWing, 7, 4),
                TestPlays.At(Position.LeftDefence, 7, 1), TestPlays.At(Position.RightDefence, 7, 3),
            },
            new[]
            {
                new Beat(TestPlays.NoMoves, PlayAction.Skate(Position.Center, new GridPoint(9, 1))),
                new Beat(TestPlays.NoMoves, PlayAction.Dump(Position.Center, new GridPoint(10, 0))),
            },
            PlayType.Faceoff,
            "offensiveLeft");

        Play mirrored = PlayMirror.Mirror(play, Rink);

        Assert.Equal("offensiveRight", mirrored.FaceoffSpotId);
        Assert.Equal(new GridPoint(9, 3), mirrored.Beats[0].Action.Target);
        Assert.Equal(new GridPoint(10, 4), mirrored.Beats[1].Action.Target);
    }

    [Fact]
    public void Mirror_Twice_GivesTheOriginalPlay()
    {
        Play play = TestPlays.PointShotScreen();

        Play twice = PlayMirror.Mirror(PlayMirror.Mirror(play, Rink), Rink);

        Assert.Equal(play.PuckCarrier, twice.PuckCarrier);
        Assert.Equal(play.StartPositions, twice.StartPositions);
        for (int i = 0; i < play.Beats.Count; i++)
        {
            Assert.Equal(play.Beats[i].Moves, twice.Beats[i].Moves);
            Assert.Equal(play.Beats[i].Action.Type, twice.Beats[i].Action.Type);
            Assert.Equal(play.Beats[i].Action.Actor, twice.Beats[i].Action.Actor);
            Assert.Equal(play.Beats[i].Action.Receiver, twice.Beats[i].Action.Receiver);
            Assert.Equal(play.Beats[i].Action.Target, twice.Beats[i].Action.Target);
        }
    }

    [Theory]
    [InlineData(1, false)] // puck on the left, written on the left (carrier LW at y = 0)
    [InlineData(2, false)] // puck on the middle lane: as written
    [InlineData(3, true)]  // puck on the right: mirrored
    [InlineData(4, true)]
    public void ShouldMirror_WhenPuckIsOnTheOtherSideThanTheWritingSide(int puckY, bool expected)
    {
        Play play = TestPlays.PointShotScreen();

        Assert.Equal(Lane.Left, PlayMirror.WritingLane(play, Rink));
        Assert.Equal(expected, PlayMirror.ShouldMirror(play, new GridPoint(6, puckY), Rink));
    }

    [Fact]
    public void ShouldMirror_IsFalse_ForPlayWrittenOnTheMiddleLane()
    {
        Play play = TestPlays.Build(
            Position.Center,
            new[]
            {
                TestPlays.At(Position.LeftWing, 5, 0), TestPlays.At(Position.Center, 5, 2), TestPlays.At(Position.RightWing, 5, 4),
                TestPlays.At(Position.LeftDefence, 3, 1), TestPlays.At(Position.RightDefence, 3, 3),
            },
            new[] { new Beat(TestPlays.NoMoves, PlayAction.Shoot(Position.Center)) });

        Assert.Equal(Lane.Middle, PlayMirror.WritingLane(play, Rink));
        Assert.False(PlayMirror.ShouldMirror(play, new GridPoint(5, 4), Rink));
    }

    [Theory]
    [InlineData("offensiveLeft", false)]
    [InlineData("offensiveRight", true)]
    [InlineData("defensiveRight", false)]
    public void ShouldMirrorAtSpot_OnlyAtTheMirrorImageSpot(string spot, bool expected)
    {
        Play play = TestPlays.Build(
            Position.Center,
            new[]
            {
                TestPlays.At(Position.LeftWing, 8, 0), TestPlays.At(Position.Center, 8, 1), TestPlays.At(Position.RightWing, 7, 4),
                TestPlays.At(Position.LeftDefence, 7, 1), TestPlays.At(Position.RightDefence, 7, 3),
            },
            new[] { new Beat(TestPlays.NoMoves, PlayAction.Shoot(Position.Center)) },
            PlayType.Faceoff,
            "offensiveLeft");

        Assert.Equal(expected, PlayMirror.ShouldMirrorAtSpot(play, spot));
    }
}
