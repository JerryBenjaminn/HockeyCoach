using System.Text.Json.Nodes;
using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Data;

public class RinkLoaderTests
{
    private static LoadResult<Rink> Load(Action<JsonObject>? edit = null)
    {
        return RinkLoader.Parse(edit == null ? Fixtures.Read("rink-small.json") : Fixtures.Edit("rink-small.json", edit));
    }

    [Fact]
    public void ValidFixture_LoadsAllFields()
    {
        LoadResult<Rink> result = Load();

        Assert.Empty(result.Errors);
        Rink rink = result.Value!;
        Assert.Equal(3, rink.Length);
        Assert.Equal(3, rink.Width);
        Assert.Equal(new GridPoint(2, 1), rink.OpponentGoal);
        Assert.Equal(new[] { "slot", "longRange" }, rink.XgZones);
        Assert.Equal("center", rink.FaceoffSpots.Single().Id);
        RinkNode node = rink.GetNode(rink.IdOf(2, 0));
        Assert.Equal(RinkZone.Offensive, rink.ZoneOf(rink.IdOf(2, 0)));
        Assert.Equal(RinkZone.Neutral, rink.ZoneOf(rink.IdOf(1, 0)));
        Assert.Equal("slot", node.XgZone);
        Assert.True(node.IsSlot);
    }

    [Fact]
    public void InvalidJson_ReportsSyntaxError()
    {
        LoadResult<Rink> result = RinkLoader.Parse("{ \"length\": 3, }");

        Assert.StartsWith("(root): invalid JSON", Assert.Single(result.Errors));
    }

    [Fact]
    public void UnsupportedSchemaVersion_IsRejected()
    {
        LoadResult<Rink> result = Load(r => r["schemaVersion"] = 2);

        Assert.Contains("schemaVersion: unsupported version 2, expected 1", result.Errors);
    }

    [Fact]
    public void MissingField_IsReportedWithPath()
    {
        LoadResult<Rink> result = Load(r => r.Remove("width"));

        Assert.Contains("width: missing", result.Errors);
    }

    [Fact]
    public void NodesOutOfIdOrder_AreRejected()
    {
        LoadResult<Rink> result = Load(r =>
        {
            JsonArray nodes = r["nodes"]!.AsArray();
            JsonNode first = nodes[0]!;
            nodes.RemoveAt(0);
            nodes.Insert(1, first);
        });

        Assert.Contains(result.Errors, e => e.StartsWith("nodes[0]: expected node (0,0) in id order", StringComparison.Ordinal));
    }

    [Fact]
    public void WrongNodeCount_IsRejected()
    {
        LoadResult<Rink> result = Load(r => r["nodes"]!.AsArray().RemoveAt(8));

        Assert.Contains("nodes: expected 9 nodes (length x width), got 8", result.Errors);
    }

    [Fact]
    public void StoredNodeZone_IsRejected()
    {
        LoadResult<Rink> result = Load(r => r["nodes"]![2]!["zone"] = "neutral");

        Assert.Contains("nodes[2].zone: unknown key", result.Errors);
    }

    [Fact]
    public void UnknownZoneId_IsRejected()
    {
        LoadResult<Rink> result = Load(r => r["zones"]![1]!["id"] = "middle");

        Assert.Contains(result.Errors, e => e.StartsWith("zones[1].id: unknown zone middle", StringComparison.Ordinal));
    }

    [Fact]
    public void ZonesNotCoveringEveryX_AreRejected()
    {
        LoadResult<Rink> result = Load(r => r["zones"]![1]!["xMax"] = 0);

        Assert.Contains("zones: x 1 belongs to 0 zones, expected exactly 1", result.Errors);
    }

    [Fact]
    public void GoalOutsideGrid_IsRejected()
    {
        LoadResult<Rink> result = Load(r => r["ownGoal"]!["y"] = 5);

        Assert.Contains(result.Errors, e => e.StartsWith("ownGoal: (0,5) is outside", StringComparison.Ordinal));
    }

    [Fact]
    public void NodeXgZoneNotListed_IsRejectedByValidator()
    {
        LoadResult<Rink> result = Load(r => r["nodes"]![4]!["xgZone"] = "crease");

        Assert.Contains("nodes[4].xgZone: crease is not listed in xgZones", result.Errors);
    }

    [Fact]
    public void FaceoffSpotOutsideGrid_IsRejected()
    {
        LoadResult<Rink> result = Load(r => r["faceoffSpots"]![0]!["x"] = 7);

        Assert.Contains(result.Errors, e => e.StartsWith("faceoffSpots[0]: (7,1) is outside", StringComparison.Ordinal));
    }

    [Fact]
    public void WrongValueType_IsReportedWithPath()
    {
        LoadResult<Rink> result = Load(r => r["nodes"]![1]!["isSlot"] = "no");

        Assert.Contains("nodes[1].isSlot: expected true or false, got a string", result.Errors);
    }

    [Fact]
    public void MetaKeys_AreIgnoredAndUnknownKeysRejected()
    {
        LoadResult<Rink> ok = Load(r => r["_comment"] = "ignored");
        LoadResult<Rink> bad = Load(r => r["lenght"] = 3);

        Assert.Empty(ok.Errors);
        Assert.Contains("lenght: unknown key", bad.Errors);
    }

    [Fact]
    public void GetOrThrow_ListsEveryErrorWithFileName()
    {
        LoadResult<Rink> result = Load(r =>
        {
            r.Remove("width");
            r["schemaVersion"] = 9;
        });

        DataLoadException ex = Assert.Throws<DataLoadException>(() => result.GetOrThrow("rink.json"));
        Assert.Contains("rink.json: width: missing", ex.Message);
        Assert.Contains("rink.json: schemaVersion: unsupported version 9, expected 1", ex.Message);
    }
}
