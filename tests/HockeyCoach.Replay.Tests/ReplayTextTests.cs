using HockeyCoach.Replay.Core;
using HockeyCoach.Replay.Tests.Support;
using Newtonsoft.Json.Linq;

namespace HockeyCoach.Replay.Tests;

public sealed class ReplayTextTests
{
    /// <summary>0: faceoff; 1: pass 1 → 2; 2: saved shot by 101; 3: away goal by 101; 4: line change 1 out, 4 in.</summary>
    private static ReplayTimeline Timeline() => new SyntheticMatch()
        .Add("Faceoff", 1, 0, 27, 1, SyntheticMatch.OnIce(27, 16, 27, 38), new JObject { ["homeCentreId"] = 1, ["awayCentreId"] = 101, ["winner"] = "Home", ["location"] = "NeutralZone" })
        .Add("Pass", 1, 4, 37, 2, SyntheticMatch.OnIce(31, 37, 27, 38), new JObject { ["passerId"] = 1, ["receiverId"] = 2, ["underPressure"] = true, ["succeeded"] = true })
        .Add("Shot", 1, 8, 12, 3, SyntheticMatch.OnIce(31, 37, 12, 37), new JObject { ["shooterId"] = 101, ["stoppedById"] = 3, ["outcome"] = "Saved", ["xg"] = 0.1234 })
        .Add("Shot", 1, 10, 12, 101, SyntheticMatch.OnIce(31, 37, 12, 37), new JObject { ["shooterId"] = 101, ["stoppedById"] = null, ["outcome"] = "Goal" })
        .Add("LineChange", 1, 10, 12, 101, new Dictionary<int, int> { [4] = 31, [2] = 37, [3] = 7, [101] = 12, [102] = 37, [103] = 47 }, new JObject { ["outgoingIds"] = new JArray(1), ["incomingIds"] = new JArray(4) })
        .Build();

    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(59.9, "00:59")]
    [InlineData(461, "07:41")]
    [InlineData(1200, "20:00")]
    [InlineData(-3, "00:00")]
    public void Clock_FormatsWholeSeconds_AsMinutesAndSeconds(double seconds, string expected)
    {
        Assert.Equal(expected, ReplayText.Clock(seconds));
    }

    [Fact]
    public void PeriodClock_ScoreAndSpeed_AreReadable()
    {
        Assert.Equal("P2 07:41", ReplayText.PeriodClock(2, 461));
        Assert.Equal("Home 1 - 0 Away", ReplayText.Score(1, 0));
        Assert.Equal("8x", ReplayText.Speed(8));
        Assert.Equal("0.5x", ReplayText.Speed(0.5));
    }

    [Fact]
    public void PlayerLabel_ShowsPositionAndId_OrOnlyTheId()
    {
        Assert.Equal("G\n3", ReplayText.PlayerLabel(new RosterEntry { Id = 3, Position = "G" }, 3));
        Assert.Equal("7", ReplayText.PlayerLabel(null, 7));
    }

    [Fact]
    public void Describe_NamesTheActorsOfEachEvent()
    {
        ReplayTimeline t = Timeline();
        Assert.Equal("Faceoff #1 C vs #101 C, won by Home (NeutralZone)", ReplayText.Describe(t.Segments[0], t));
        Assert.Equal("Pass #1 C -> #2 LD, completed, under pressure", ReplayText.Describe(t.Segments[1], t));
        Assert.Equal("Shot by #101 C: Saved by #3 G, xG 0.12", ReplayText.Describe(t.Segments[2], t));
        Assert.Equal("Shot by #101 C: GOAL (Away)", ReplayText.Describe(t.Segments[3], t));
        Assert.Equal("Line change: out #1 C; in #4 LW", ReplayText.Describe(t.Segments[4], t));
    }

    [Fact]
    public void EventHeading_IsOneBased()
    {
        ReplayTimeline t = Timeline();
        Assert.Equal("Event 2/5 Pass", ReplayText.EventHeading(t.Segments[1], t.Segments.Count));
    }
}
