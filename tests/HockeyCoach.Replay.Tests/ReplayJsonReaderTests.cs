using HockeyCoach.Replay.Core;
using HockeyCoach.Replay.Tests.Support;
using Newtonsoft.Json.Linq;

namespace HockeyCoach.Replay.Tests;

public sealed class ReplayJsonReaderTests
{
    private static SyntheticMatch OneEvent()
    {
        return new SyntheticMatch().Add("Faceoff", 1, 0, 27, null, SyntheticMatch.OnIce(27, 16, 27, 38),
            new JObject { ["homeCentreId"] = 1, ["awayCentreId"] = 101, ["winner"] = "Home", ["location"] = "NeutralZone" });
    }

    private static ReplayFormatException Fails(Action<JObject> edit)
    {
        SyntheticMatch m = OneEvent();
        edit(m.Root);
        return Assert.Throws<ReplayFormatException>(() => ReplayJsonReader.Read(m.Json));
    }

    [Fact]
    public void Read_ParsesHeaderAndEvents()
    {
        ReplayMatch m = ReplayJsonReader.Read(OneEvent().Json);
        Assert.Equal(1, m.FormatVersion);
        Assert.Equal(1200, m.PeriodSeconds);
        Assert.Equal(3, m.PeriodCount);
        Assert.Equal(8, m.Roster.Count);
        Assert.Equal("G", m.Roster.Single(r => r.Id == 3).Position);
        Assert.True(m.Roster.Single(r => r.Id == 3).Starter);
        Assert.Equal(11, m.Rink.Length);
        Assert.Equal(55, m.Rink.Nodes.Count);
        Assert.Equal(22.86, m.Rink.Metres.BlueLineFromEnd);
        ReplayEvent e = Assert.Single(m.Events);
        Assert.Equal(ReplayEventType.Faceoff, e.Kind);
        Assert.Equal(101, e.AwayCentreId);
        Assert.Equal(6, e.Placement.Players.Count);
        Assert.Null(e.Placement.PuckCarrierId);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(0)]
    public void Read_RejectsUnknownFormatVersion_WithAClearMessage(int version)
    {
        ReplayFormatException e = Fails(r => r["formatVersion"] = version);
        Assert.Contains("Unsupported match JSON formatVersion " + version, e.Message);
        Assert.Contains("reads formatVersion 1", e.Message);
    }

    [Fact]
    public void Read_RejectsNonIntegerFormatVersion()
    {
        ReplayFormatException e = Fails(r => r["formatVersion"] = "1");
        Assert.Contains("Unsupported match JSON formatVersion \"1\"", e.Message);
    }

    [Fact]
    public void Read_RejectsMissingFormatVersion_AsAnOldExport()
    {
        ReplayFormatException e = Fails(r => r.Remove("formatVersion"));
        Assert.Contains("no formatVersion", e.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{ not json")]
    [InlineData("[1, 2]")]
    public void Read_RejectsTextThatIsNotAJsonObject(string json)
    {
        Assert.Throws<ReplayFormatException>(() => ReplayJsonReader.Read(json));
    }

    [Fact]
    public void Read_RejectsNull()
    {
        Assert.Throws<ReplayFormatException>(() => ReplayJsonReader.Read(null!));
    }

    [Fact]
    public void Read_RejectsMissingRink()
    {
        Assert.Contains("no rink", Fails(r => r.Remove("rink")).Message);
    }

    [Fact]
    public void Read_RejectsMissingMetres()
    {
        Assert.Contains("metres", Fails(r => ((JObject)r["rink"]!).Remove("metres")).Message);
    }

    [Fact]
    public void Read_RejectsInconsistentMetres()
    {
        Assert.Contains("inconsistent", Fails(r => r["rink"]!["metres"]!["blueLineFromEnd"] = 2.0).Message);
    }

    [Fact]
    public void Read_RejectsMissingRoster()
    {
        Assert.Contains("roster", Fails(r => r.Remove("roster")).Message);
    }

    [Fact]
    public void Read_RejectsNoEvents()
    {
        Assert.Contains("no events", Fails(r => r["events"] = new JArray()).Message);
    }

    [Fact]
    public void Read_RejectsUnknownEventType()
    {
        Assert.Contains("unknown type 'Penalty'", Fails(r => r["events"]![0]!["type"] = "Penalty").Message);
    }

    [Fact]
    public void Read_RejectsNumericEventType()
    {
        Assert.Contains("unknown type '3'", Fails(r => r["events"]![0]!["type"] = "3").Message);
    }

    [Fact]
    public void Read_RejectsEventWithoutPlacement()
    {
        Assert.Contains("no placement", Fails(r => ((JObject)r["events"]![0]!).Remove("placement")).Message);
    }

    [Fact]
    public void Read_RejectsNodesOutsideTheGrid()
    {
        Assert.Contains("outside the rink grid", Fails(r => r["events"]![0]!["placement"]!["puckNodeId"] = 55).Message);
    }

    [Fact]
    public void Read_RejectsUnknownTeam()
    {
        Assert.Contains("Unknown team 'Visitors'", Fails(r => r["events"]![0]!["placement"]!["players"]![0]!["team"] = "Visitors").Message);
    }

    [Fact]
    public void Read_RejectsWrongFieldTypes_AsFormatErrors()
    {
        Assert.Contains("does not match the export format", Fails(r => r["periodSeconds"] = "twenty minutes").Message);
    }
}
