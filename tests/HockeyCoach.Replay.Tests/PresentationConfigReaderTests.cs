using HockeyCoach.Replay.Core;
using HockeyCoach.Replay.Tests.Support;
using Newtonsoft.Json.Linq;

namespace HockeyCoach.Replay.Tests;

/// <summary>The strict reader of <c>data/replay.json</c> (D-088 B).</summary>
public sealed class PresentationConfigReaderTests
{
    private static readonly string[] Sections = { "timing", "goal", "puck", "skaters", "goalies", "bench" };

    private static JObject Real() => JObject.Parse(ReplayConfigFile.Text);

    private static PresentationConfigException Fails(Action<JObject> edit)
    {
        JObject root = Real();
        edit(root);
        return Assert.Throws<PresentationConfigException>(() => PresentationConfigReader.Read(root.ToString()));
    }

    public static TheoryData<string> AllKeyPaths()
    {
        var data = new TheoryData<string>();
        JObject root = Real();
        foreach (string section in Sections)
        {
            foreach (JProperty p in ((JObject)root[section]!).Properties().Where(p => !p.Name.StartsWith('_')))
            {
                data.Add(section + "." + p.Name);
            }
        }

        return data;
    }

    [Fact]
    public void Read_AcceptsTheRealReplayJson_WithItsValues()
    {
        PresentationConfig c = PresentationConfigReader.Read(ReplayConfigFile.Text);
        JObject root = Real();

        Assert.Equal((double)root["timing"]!["actionCompression"]!, c.Timing.ActionCompression);
        Assert.Equal((double)root["timing"]!["deadCompression"]!, c.Timing.DeadCompression);
        Assert.Equal((double)root["timing"]!["faceoffApproachMaxSeconds"]!, c.Timing.FaceoffApproachMaxSeconds);
        Assert.Equal((double)root["goal"]!["slowMoRate"]!, c.Goal.SlowMoRate);
        Assert.Equal((double)root["puck"]!["carriedHeadingMinSpeed"]!, c.Puck.CarriedHeadingMinSpeed);
        Assert.Equal((int)root["skaters"]!["separationIterations"]!, c.Skaters.SeparationIterations);
        Assert.Equal((double)root["skaters"]!["idleYFrequencyRatio"]!, c.Skaters.IdleYFrequencyRatio);
        Assert.Equal((double)root["goalies"]!["lateralMaxMetres"]!, c.Goalies.LateralMaxMetres);
        Assert.Equal((double)root["bench"]!["homeGateFromCentreMetres"]!, c.Bench.HomeGateFromCentreMetres);
        Assert.Equal((double)root["bench"]!["gateYMetres"]!, c.Bench.GateYMetres);
    }

    [Fact]
    public void Read_FillsEveryKeyOfTheRealFile()
    {
        PresentationConfig c = PresentationConfigReader.Read(ReplayConfigFile.Text);
        var objects = new Dictionary<string, object>
        {
            ["timing"] = c.Timing, ["goal"] = c.Goal, ["puck"] = c.Puck, ["skaters"] = c.Skaters, ["goalies"] = c.Goalies, ["bench"] = c.Bench,
        };
        JObject root = Real();
        foreach (string section in Sections)
        {
            foreach (JProperty p in ((JObject)root[section]!).Properties().Where(p => !p.Name.StartsWith('_')))
            {
                string name = char.ToUpperInvariant(p.Name[0]) + p.Name[1..];
                var property = objects[section].GetType().GetProperty(name);
                Assert.True(property != null, section + "." + p.Name + " has no property");
                Assert.Equal((double)p.Value, Convert.ToDouble(property!.GetValue(objects[section])));
            }
        }
    }

    [Fact]
    public void CheckRink_AcceptsTheRealFileOnTheMatchRink()
    {
        PresentationConfig c = PresentationConfigReader.Read(ReplayConfigFile.Text);
        PresentationConfigReader.CheckRink(c, NhlMetres());
    }

    [Theory]
    [MemberData(nameof(AllKeyPaths))]
    public void Read_RejectsAMissingKey_NamingIt(string path)
    {
        string[] parts = path.Split('.');
        PresentationConfigException e = Fails(r => ((JObject)r[parts[0]]!).Remove(parts[1]));
        Assert.Contains("missing key " + path, e.Message);
    }

    [Theory]
    [InlineData("timing")]
    [InlineData("goal")]
    [InlineData("puck")]
    [InlineData("skaters")]
    [InlineData("goalies")]
    [InlineData("bench")]
    public void Read_RejectsAMissingSection(string section)
    {
        Assert.Contains("missing key " + section, Fails(r => r.Remove(section)).Message);
    }

    [Theory]
    [InlineData("timing")]
    [InlineData("goal")]
    [InlineData("puck")]
    [InlineData("skaters")]
    [InlineData("goalies")]
    [InlineData("bench")]
    public void Read_RejectsAnUnknownKeyInASection_NamingIt(string section)
    {
        PresentationConfigException e = Fails(r => r[section]!["speedOfLight"] = 1);
        Assert.Contains("unknown key " + section + ".speedOfLight", e.Message);
    }

    [Fact]
    public void Read_RejectsAnUnknownTopLevelKey()
    {
        Assert.Contains("unknown key camera", Fails(r => r["camera"] = new JObject()).Message);
    }

    [Fact]
    public void Read_IgnoresMetaKeysStartingWithUnderscore()
    {
        JObject root = Real();
        root["_extra"] = "note";
        root["timing"]!["_notes"] = "note";
        root["skaters"]!["_why"] = new JObject { ["a"] = 1 };
        PresentationConfigReader.Read(root.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Read_RejectsAWrongSchemaVersion(int version)
    {
        PresentationConfigException e = Fails(r => r["schemaVersion"] = version);
        Assert.Contains("schemaVersion is " + version, e.Message);
    }

    [Fact]
    public void Read_RejectsAStringSchemaVersion()
    {
        Assert.Contains("schemaVersion", Fails(r => r["schemaVersion"] = "1").Message);
    }

    [Fact]
    public void Read_RejectsAMissingSchemaVersion()
    {
        Assert.Contains("missing key schemaVersion", Fails(r => r.Remove("schemaVersion")).Message);
    }

    [Theory]
    [InlineData("timing.actionCompression", 0)]
    [InlineData("timing.actionCompression", 20.5)]
    [InlineData("timing.deadCompression", 2.9)]
    [InlineData("timing.deadCompression", 51)]
    [InlineData("timing.minEventSeconds", -0.1)]
    [InlineData("timing.minEventSeconds", 5.1)]
    [InlineData("timing.lineChangeSeconds", 6)]
    [InlineData("timing.stoppageHoldSeconds", -1)]
    [InlineData("timing.timeoutHoldSeconds", 11)]
    [InlineData("timing.periodBreakSeconds", 31)]
    [InlineData("timing.faceoffApproachSpeed", 0)]
    [InlineData("timing.faceoffApproachSpeed", 16)]
    [InlineData("timing.faceoffApproachMinSeconds", 2.5)]
    [InlineData("timing.faceoffApproachMinSeconds", -1)]
    [InlineData("timing.faceoffApproachMaxSeconds", 0)]
    [InlineData("timing.faceoffApproachMaxSeconds", 11)]
    [InlineData("goal.slowMoRate", 0)]
    [InlineData("goal.slowMoRate", 1.01)]
    [InlineData("goal.slowMoMinSeconds", 11)]
    [InlineData("goal.holdSeconds", -1)]
    [InlineData("goal.netInsideMetres", 1.13)]
    [InlineData("puck.passSpeed", 4.9)]
    [InlineData("puck.passSpeed", 51)]
    [InlineData("puck.longPassSpeed", 19)]
    [InlineData("puck.longPassMinMetres", 0)]
    [InlineData("puck.shotSpeed", 9)]
    [InlineData("puck.dumpInSpeed", 4)]
    [InlineData("puck.reboundSpeed", 0)]
    [InlineData("puck.blockDeflectSpeed", 0)]
    [InlineData("puck.loosePuckSpeed", 51)]
    [InlineData("puck.faceoffDrawSpeed", 0)]
    [InlineData("puck.faceoffDrawDelaySeconds", 2.1)]
    [InlineData("puck.missWideMetres", 5.1)]
    [InlineData("puck.carriedLeadMetres", -0.1)]
    [InlineData("puck.carriedHeadingMinSpeed", -0.1)]
    [InlineData("skaters.topSpeed", 2.9)]
    [InlineData("skaters.topSpeed", 12.1)]
    [InlineData("skaters.acceleration", 0)]
    [InlineData("skaters.deceleration", 21)]
    [InlineData("skaters.pathAlpha", 1.1)]
    [InlineData("skaters.pathTension", -0.1)]
    [InlineData("skaters.stopTurnDegrees", 181)]
    [InlineData("skaters.anticipationSeconds", 2.1)]
    [InlineData("skaters.nodeOffsetRadiusMetres", 2.6)]
    [InlineData("skaters.separationMinMetres", 4.1)]
    [InlineData("skaters.separationIterations", -1)]
    [InlineData("skaters.separationIterations", 11)]
    [InlineData("skaters.separationIterations", 2.5)]
    [InlineData("skaters.idleAmplitudeMetres", 1.1)]
    [InlineData("skaters.idleFrequencyHz", -0.1)]
    [InlineData("skaters.idleYFrequencyRatio", 0)]
    [InlineData("goalies.depthMetres", 3.1)]
    [InlineData("goalies.lateralTrackFactor", 1.1)]
    [InlineData("goalies.lateralMaxMetres", -0.1)]
    public void Read_RejectsAnOutOfRangeValue_NamingTheKey(string path, double value)
    {
        string[] parts = path.Split('.');
        PresentationConfigException e = Fails(r => r[parts[0]]![parts[1]] = value);
        Assert.Contains(path + " is ", e.Message);
    }

    [Theory]
    [InlineData("timing.actionCompression", 6.0)]
    [InlineData("timing.minEventSeconds", 0)]
    [InlineData("timing.deadCompression", 3.0)]
    [InlineData("timing.faceoffApproachMinSeconds", 2.0)]
    [InlineData("goal.slowMoRate", 1)]
    [InlineData("goal.netInsideMetres", 1.12)]
    [InlineData("puck.longPassSpeed", 20)]
    [InlineData("skaters.separationIterations", 0)]
    [InlineData("skaters.separationIterations", 10)]
    [InlineData("skaters.anticipationSeconds", 0)]
    public void Read_AcceptsBoundaryValues(string path, double value)
    {
        string[] parts = path.Split('.');
        JObject root = Real();
        root[parts[0]]![parts[1]] = value;
        PresentationConfigReader.Read(root.ToString());
    }

    [Theory]
    [InlineData("\"fast\"")]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("[1]")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    public void Read_RejectsNonFiniteOrNonNumericValues_NamingTheKey(string raw)
    {
        string text = ReplayConfigFile.Text.Replace("\"shotSpeed\": 32.0", "\"shotSpeed\": " + raw);
        Assert.NotEqual(ReplayConfigFile.Text, text);
        PresentationConfigException e = Assert.Throws<PresentationConfigException>(() => PresentationConfigReader.Read(text));
        Assert.Contains("puck.shotSpeed", e.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ nope")]
    [InlineData("[1]")]
    public void Read_RejectsTextThatIsNotAJsonObject(string text)
    {
        Assert.Throws<PresentationConfigException>(() => PresentationConfigReader.Read(text));
    }

    [Fact]
    public void Read_RejectsASectionThatIsNotAnObject()
    {
        Assert.Contains("goal must be an object", Fails(r => r["goal"] = 3).Message);
    }

    [Theory]
    [InlineData("homeGateFromCentreMetres", -30.6)]
    [InlineData("awayGateFromCentreMetres", 30.6)]
    [InlineData("gateYMetres", -3.1)]
    [InlineData("gateYMetres", 29.0)]
    public void CheckRink_RejectsBenchValuesOutsideTheRink(string key, double value)
    {
        JObject root = Real();
        root["bench"]![key] = value;
        PresentationConfig c = PresentationConfigReader.Read(root.ToString());
        PresentationConfigException e = Assert.Throws<PresentationConfigException>(() => PresentationConfigReader.CheckRink(c, NhlMetres()));
        Assert.Contains("bench." + key, e.Message);
    }

    [Theory]
    [InlineData("homeGateFromCentreMetres", -30.48)]
    [InlineData("gateYMetres", -3)]
    [InlineData("gateYMetres", 28.91)]
    public void CheckRink_AcceptsBenchValuesOnTheLimit(string key, double value)
    {
        JObject root = Real();
        root["bench"]![key] = value;
        PresentationConfigReader.CheckRink(PresentationConfigReader.Read(root.ToString()), NhlMetres());
    }

    private static RinkMetresData NhlMetres() => new() { Length = 60.96, Width = 25.91, GoalLineFromEnd = 3.35, BlueLineFromEnd = 22.86 };
}
