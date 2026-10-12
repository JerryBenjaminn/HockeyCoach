using HockeyCoach.Replay.Core;
using HockeyCoach.Replay.Tests.Support;
using Newtonsoft.Json.Linq;

namespace HockeyCoach.Replay.Tests;

public sealed class TimelineBuilderTests
{
    private static JObject Faceoff() => new() { ["homeCentreId"] = 1, ["awayCentreId"] = 101, ["winner"] = "Home", ["location"] = "NeutralZone" };

    private static JObject Pass(int passer, int receiver, bool succeeded = true) =>
        new() { ["passerId"] = passer, ["receiverId"] = receiver, ["underPressure"] = false, ["succeeded"] = succeeded };

    private static JObject Shot(int shooter, int? stoppedBy, string outcome) =>
        new() { ["shooterId"] = shooter, ["stoppedById"] = stoppedBy, ["xg"] = 0.1, ["chanceClass"] = null, ["chanceType"] = "Rush", ["underPressure"] = false, ["speed"] = null, ["outcome"] = outcome };

    private static JObject Change(int[] outgoing, int[] incoming) => new() { ["outgoingIds"] = new JArray(outgoing), ["incomingIds"] = new JArray(incoming) };

    [Fact]
    public void Faceoff_StartsAtTheSpotUnderTheCentres()
    {
        ReplayTimeline t = new SyntheticMatch().Add("Faceoff", 1, 2, 16, 2, SyntheticMatch.OnIce(27, 16, 27, 38), Faceoff()).Build();
        TimelineSegment s = t.Segments[0];
        Assert.Equal("center", s.FaceoffSpot!.Id);
        Assert.Equal(27, s.Puck.StartNodeId);
        Assert.Equal(16, s.Puck.EndNodeId);
        Assert.Equal(0, s.StartClock);
        Assert.Equal(2, s.EndClock);
    }

    [Fact]
    public void Faceoff_OffTheSpots_HasNoSpot_AndStartsFromThePreviousPuck()
    {
        ReplayTimeline t = new SyntheticMatch()
            .Add("PuckBattle", 1, 1, 30, 1, SyntheticMatch.OnIce(30, 16, 31, 38), new JObject { ["attackerIds"] = new JArray(1), ["defenderIds"] = new JArray(101), ["outcome"] = "Win" })
            .Add("Faceoff", 1, 2, 16, 2, SyntheticMatch.OnIce(28, 16, 28, 38), Faceoff())
            .Build();
        Assert.Null(t.Segments[1].FaceoffSpot);
        Assert.Equal(30, t.Segments[1].Puck.StartNodeId);
    }

    [Fact]
    public void Pass_StartsAtThePassersPreviousNode_AndEndsAtThePlacementPuck()
    {
        ReplayTimeline t = new SyntheticMatch()
            .Add("Faceoff", 1, 0, 27, 1, SyntheticMatch.OnIce(27, 16, 27, 38), Faceoff())
            .Add("Pass", 1, 3, 21, 2, SyntheticMatch.OnIce(32, 21, 33, 38), Pass(1, 2))
            .Build();
        TimelineSegment s = t.Segments[1];
        Assert.Equal(27, s.Puck.StartNodeId);
        Assert.Equal(21, s.Puck.EndNodeId);
        Assert.Null(s.Puck.Via);
        Assert.Equal(2, s.Puck.CarrierId);
        SegmentPlayer passer = s.Players.Single(p => p.Id == 1);
        Assert.Equal(27, passer.FromNodeId);
        Assert.Equal(32, passer.NodeId);
    }

    [Fact]
    public void AwayGoal_GoesIntoTheHomeNet_AndCountsForAway()
    {
        ReplayTimeline t = new SyntheticMatch()
            .Add("Faceoff", 1, 0, 12, 101, SyntheticMatch.OnIce(27, 16, 12, 38), Faceoff())
            .Add("Shot", 1, 5, 12, 101, SyntheticMatch.OnIce(27, 16, 12, 38), Shot(101, null, "Goal"))
            .Add("Stoppage", 1, 5, 12, 101, SyntheticMatch.OnIce(27, 16, 12, 38), new JObject { ["reason"] = "Goal" })
            .Build();
        TimelineSegment s = t.Segments[1];
        Assert.Equal(ReplayTeam.Away, s.ScoringTeam);
        Assert.Equal(t.Mapping.HomeNet, s.Puck.Via);
        Assert.Equal(7, s.Puck.ViaNodeId);
        Assert.Equal((0, 0), (s.HomeScoreBefore, s.AwayScoreBefore));
        Assert.Equal((0, 1), (s.HomeScore, s.AwayScore));
        Assert.Equal((0, 1), (t.HomeScore, t.AwayScore));
        TimelineGoal g = Assert.Single(t.Goals);
        Assert.Equal((1, 5.0, ReplayTeam.Away, 101, 1), (g.Period, g.Time, g.Team, g.ShooterId, g.SegmentIndex));
    }

    [Fact]
    public void HomeGoal_GoesIntoTheAwayNet()
    {
        ReplayTimeline t = new SyntheticMatch()
            .Add("Shot", 1, 5, 42, 1, SyntheticMatch.OnIce(42, 16, 12, 38), Shot(1, null, "Goal"))
            .Build();
        Assert.Equal(t.Mapping.AwayNet, t.Segments[0].Puck.Via);
        Assert.Equal((1, 0), (t.HomeScore, t.AwayScore));
    }

    [Fact]
    public void BlockedShot_StopsAtTheBlocker_AndSavedShotAtTheGoalie()
    {
        ReplayTimeline t = new SyntheticMatch()
            .Add("Faceoff", 1, 0, 43, 1, SyntheticMatch.OnIce(43, 38, 43, 42), Faceoff())
            .Add("Shot", 1, 2, 42, null, SyntheticMatch.OnIce(43, 38, 43, 42), Shot(1, 102, "Blocked"))
            .Add("Shot", 1, 4, 43, 1, SyntheticMatch.OnIce(43, 38, 43, 37), Shot(2, 103, "Saved"))
            .Build();
        Assert.Equal(42, t.Segments[1].Puck.ViaNodeId);
        Assert.Equal(t.Mapping.NodePosition(42), t.Segments[1].Puck.Via);
        Assert.Equal(43, t.Segments[1].Puck.StartNodeId);
        Assert.Equal(42, t.Segments[1].Puck.EndNodeId);
        Assert.Equal(47, t.Segments[2].Puck.ViaNodeId);
        Assert.Equal(38, t.Segments[2].Puck.StartNodeId);
        Assert.Null(t.Segments[2].ScoringTeam);
    }

    [Fact]
    public void LineChanges_AtOneStoppage_ApplyEachChangeOnItsOwn()
    {
        Dictionary<int, int> after = SyntheticMatch.OnIce(25, 16, 29, 38);
        after.Remove(1);
        after.Remove(101);
        after[4] = 27;
        after[104] = 27;
        ReplayTimeline t = new SyntheticMatch()
            .Add("Stoppage", 1, 10, 33, null, SyntheticMatch.OnIce(33, 16, 32, 38), new JObject { ["reason"] = "OutOfPlay" })
            .Add("LineChange", 1, 10, 27, null, after, Change(new[] { 1 }, new[] { 4 }))
            .Add("LineChange", 1, 10, 27, null, after, Change(new[] { 101 }, new[] { 104 }))
            .Build();
        TimelineSegment first = t.Segments[1];
        Assert.Equal(new[] { 2, 3, 4, 101, 102, 103 }, first.Players.Select(p => p.Id));
        Assert.Equal(new[] { 1 }, first.Leaving);
        Assert.Equal(new[] { 4 }, first.Entering);
        SegmentPlayer stillOn = first.Players.Single(p => p.Id == 101);
        Assert.Equal(32, stillOn.NodeId);
        SegmentPlayer entering = first.Players.Single(p => p.Id == 4);
        Assert.Equal(27, entering.FromNodeId);
        Assert.Equal(27, entering.NodeId);

        TimelineSegment second = t.Segments[2];
        Assert.Equal(new[] { 2, 3, 4, 102, 103, 104 }, second.Players.Select(p => p.Id));
        Assert.Equal(new[] { 101 }, second.Leaving);
        Assert.Equal(new[] { 104 }, second.Entering);
        Assert.Equal(0, second.Duration);
    }

    [Fact]
    public void LineChange_InconsistentWithTheIce_FallsBackToThePlacement()
    {
        Dictionary<int, int> after = SyntheticMatch.OnIce(25, 16, 29, 38);
        after.Remove(1);
        after[4] = 27;
        ReplayTimeline t = new SyntheticMatch()
            .Add("Stoppage", 1, 10, 33, null, SyntheticMatch.OnIce(33, 16, 32, 38), new JObject { ["reason"] = "OutOfPlay" })
            .Add("LineChange", 1, 10, 27, null, after, Change(new[] { 104 }, new[] { 4 }))
            .Build();
        Assert.Equal(after.Keys.OrderBy(i => i), t.Segments[1].Players.Select(p => p.Id));
    }

    [Fact]
    public void Clock_CountsPeriodsFromTheMatchStart()
    {
        ReplayTimeline t = new SyntheticMatch()
            .Add("Stoppage", 1, 1200, 27, null, SyntheticMatch.OnIce(27, 16, 27, 38), new JObject { ["reason"] = "PeriodEnd" })
            .Add("Faceoff", 2, 2.5, 27, null, SyntheticMatch.OnIce(27, 16, 27, 38), Faceoff())
            .Build();
        Assert.Equal(1200, t.Segments[1].StartClock);
        Assert.Equal(1202.5, t.Segments[1].EndClock);
        Assert.Equal(1202.5, t.Duration);
    }

    [Fact]
    public void Build_Throws_WhenTheClockGoesBackwards()
    {
        SyntheticMatch m = new SyntheticMatch()
            .Add("Faceoff", 1, 10, 27, null, SyntheticMatch.OnIce(27, 16, 27, 38), Faceoff())
            .Add("Faceoff", 1, 9, 27, null, SyntheticMatch.OnIce(27, 16, 27, 38), Faceoff());
        ReplayFormatException e = Assert.Throws<ReplayFormatException>(() => m.Build());
        Assert.Contains("earlier than the previous event", e.Message);
    }
}
