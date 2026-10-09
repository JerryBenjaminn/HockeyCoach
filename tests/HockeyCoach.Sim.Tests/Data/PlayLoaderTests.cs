using System.Text.Json.Nodes;
using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Data;

public class PlayLoaderTests
{
    private const string Example = """
        {
          "schemaVersion": 1,
          "id": "pointShotScreen",
          "name": "Point shot with screen",
          "_notes": "ignored",
          "type": "offensiveZone",
          "start": {
            "puckCarrier": "LW",
            "positions": { "LW": [8, 0], "C": [8, 3], "RW": [9, 4], "LD": [7, 1], "RD": [7, 3] }
          },
          "beats": [
            { "moves": { "C": [9, 3] }, "action": { "type": "pass", "from": "LW", "to": "LD" } },
            { "moves": { "LW": [9, 1] }, "action": { "type": "driveNet", "by": "RW" } },
            { "moves": {}, "action": { "type": "shoot", "by": "LD" } }
          ]
        }
        """;

    private static readonly Rink Rink = TestRinks.Standard();
    private static readonly PlaysConfig Limits = new(4, 2);

    private static LoadResult<Play> Load(Action<JsonObject>? edit = null, string fileId = "pointShotScreen")
    {
        string json = Example;
        if (edit != null)
        {
            JsonObject root = JsonNode.Parse(json)!.AsObject();
            edit(root);
            json = root.ToJsonString();
        }

        return PlayLoader.Parse(json, fileId, Rink, Limits);
    }

    [Fact]
    public void SchemaExample_Loads()
    {
        LoadResult<Play> result = Load();

        Assert.Empty(result.Errors);
        Play play = result.Value!;
        Assert.Equal("pointShotScreen", play.Id);
        Assert.Equal(PlayType.OffensiveZone, play.Type);
        Assert.Equal(Position.LeftWing, play.PuckCarrier);
        Assert.Equal(new GridPoint(9, 4), play.StartPositions[Position.RightWing]);
        Assert.Equal(3, play.Beats.Count);
        Assert.Equal(new GridPoint(9, 3), play.Beats[0].Moves[Position.Center]);
        Assert.Equal(Position.LeftDefence, play.Beats[0].Action.Receiver);
        Assert.Equal(PlayActionType.DriveNet, play.Beats[1].Action.Type);
    }

    [Fact]
    public void IdNotMatchingFileName_IsRejected()
    {
        Assert.Contains("id: id pointShotScreen does not match the file name other", Load(fileId: "other").Errors);
    }

    [Fact]
    public void MirrorableFlag_IsAnUnknownKey()
    {
        Assert.Contains("mirrorable: unknown key", Load(r => r["mirrorable"] = true).Errors);
    }

    [Fact]
    public void UnknownTypePositionAndAction_AreRejected()
    {
        Assert.Contains(Load(r => r["type"] = "cycle").Errors, e => e.StartsWith("type: unknown play type cycle", StringComparison.Ordinal));
        Assert.Contains(Load(r => r["start"]!["puckCarrier"] = "G").Errors, e => e.StartsWith("start.puckCarrier: unknown position G", StringComparison.Ordinal));
        Assert.Contains(Load(r => r["beats"]![2]!["action"]!["type"] = "deke").Errors, e => e.StartsWith("beats[2].action.type: unknown action type deke", StringComparison.Ordinal));
    }

    [Fact]
    public void ActionWithFieldsOfAnotherType_IsRejected()
    {
        Assert.Contains("beats[2].action.to: unknown key", Load(r => r["beats"]![2]!["action"]!["to"] = new JsonArray(9, 1)).Errors);
    }

    [Fact]
    public void MissingAction_IsRejected()
    {
        Assert.Contains("beats[0].action: missing", Load(r => r["beats"]![0]!.AsObject().Remove("action")).Errors);
    }

    [Fact]
    public void CoordinateMustBeAnXYPair()
    {
        Assert.Contains("start.positions.LW: expected [x, y], got 3 values", Load(r => r["start"]!["positions"]!["LW"] = new JsonArray(8, 0, 1)).Errors);
    }

    [Fact]
    public void ValidatorErrors_AreReportedAfterMapping()
    {
        LoadResult<Play> result = Load(r => r["start"]!["positions"]!["RW"] = new JsonArray(9, 2));

        Assert.Contains(result.Errors, e => e.StartsWith("start.positions.RW: (9,2) is a goal node", StringComparison.Ordinal));
    }

    [Fact]
    public void FaceoffSpot_IsRead()
    {
        LoadResult<Play> result = Load(r =>
        {
            r["type"] = "faceoff";
            r["faceoffSpot"] = "offensiveLeft";
        });

        Assert.Empty(result.Errors);
        Assert.Equal("offensiveLeft", result.Value!.FaceoffSpotId);
    }
}
