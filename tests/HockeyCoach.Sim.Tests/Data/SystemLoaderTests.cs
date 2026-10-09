using System.Text.Json.Nodes;
using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Data;

public class SystemLoaderTests
{
    private const string Example = """
        {
          "schemaVersion": 1,
          "id": "trap122",
          "name": "1-2-2 trap",
          "_notes": "ignored",
          "mirrorY": true,
          "rules": [
            {
              "when": { "puckZones": ["offensive"], "puckX": [7, 10], "puckY": [0, 2], "puckState": "controlled" },
              "targets": {
                "F1": { "puckOffset": [0, 0] }, "F2": { "node": [6, 1] }, "F3": { "node": [6, 3] },
                "D1": { "node": [4, 1] }, "D2": { "node": [4, 3] }
              }
            },
            {
              "when": {},
              "targets": {
                "F1": { "puckOffset": [-1, 0] }, "F2": { "node": [3, 1] }, "F3": { "node": [3, 3] },
                "D1": { "node": [2, 1] }, "D2": { "node": [2, 2] }
              }
            }
          ]
        }
        """;

    private static readonly Rink Rink = TestRinks.Standard();

    private static LoadResult<DefensiveSystem> Load(Action<JsonObject>? edit = null)
    {
        string json = Example;
        if (edit != null)
        {
            JsonObject root = JsonNode.Parse(json)!.AsObject();
            edit(root);
            json = root.ToJsonString();
        }

        return SystemLoader.Parse(json, "trap122", Rink);
    }

    private static JsonObject When(JsonObject root, int rule) => root["rules"]![rule]!["when"]!.AsObject();

    [Fact]
    public void SchemaExample_Loads()
    {
        LoadResult<DefensiveSystem> result = Load();

        Assert.Empty(result.Errors);
        DefensiveSystem system = result.Value!;
        Assert.True(system.MirrorY);
        Assert.Equal(2, system.Rules.Count);
        SystemCondition when = system.Rules[0].When;
        Assert.Equal(new[] { RinkZone.Offensive }, when.PuckZones);
        Assert.Equal(7, when.PuckX!.Min);
        Assert.Equal(2, when.PuckY!.Max);
        Assert.Equal(PuckState.Controlled, when.PuckState);
        Assert.True(system.Rules[0].Targets[SystemRole.F1].IsPuckOffset);
        Assert.Equal(new GridPoint(-1, 0), system.Rules[1].Targets[SystemRole.F1].Value);
        Assert.True(system.Rules[1].When.IsEmpty);
    }

    [Fact]
    public void UnknownNames_AreRejected()
    {
        Assert.Contains(Load(r => When(r, 0)["puckZones"] = new JsonArray("slot")).Errors, e => e.StartsWith("rules[0].when.puckZones[0]: unknown zone slot", StringComparison.Ordinal));
        Assert.Contains(Load(r => When(r, 0)["puckState"] = "held").Errors, e => e.StartsWith("rules[0].when.puckState: unknown puck state held", StringComparison.Ordinal));
        Assert.Contains("rules[0].when.puckSide: unknown key", Load(r => When(r, 0)["puckSide"] = "left").Errors);
        Assert.Contains(Load(r => r["rules"]![0]!["targets"]!["F4"] = new JsonObject { ["node"] = new JsonArray(3, 3) }).Errors, e => e.StartsWith("rules[0].targets.F4: unknown role F4", StringComparison.Ordinal));
    }

    [Fact]
    public void TargetWithBothOrNeitherKind_IsRejected()
    {
        Assert.Contains(
            "rules[0].targets.F2: target must have exactly one of node and puckOffset",
            Load(r => r["rules"]![0]!["targets"]!["F2"]!["puckOffset"] = new JsonArray(0, 1)).Errors);
        Assert.Contains(
            "rules[0].targets.F2: target must have exactly one of node and puckOffset",
            Load(r => r["rules"]![0]!["targets"]!["F2"] = new JsonObject()).Errors);
    }

    [Fact]
    public void ValidatorErrors_AreReportedAfterMapping()
    {
        LoadResult<DefensiveSystem> result = Load(r => r["rules"]![1]!["targets"]!["D2"]!["node"] = new JsonArray(1, 2));

        Assert.Contains(result.Errors, e => e.StartsWith("rules[1].targets.D2.node: (1,2) is a goal node", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingMirrorY_IsRejected()
    {
        Assert.Contains("mirrorY: missing", Load(r => r.Remove("mirrorY")).Errors);
    }
}
