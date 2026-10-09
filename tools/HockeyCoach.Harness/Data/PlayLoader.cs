using System.Text.Json;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Harness.Data;

/// <summary>
/// Loads data/plays/*.json (docs/data-schema.md, Kuviot): JSON → path-checked mapping → <see cref="Play"/> →
/// <see cref="PlayValidator"/>. One play per file; the id must equal the file name.
/// </summary>
public static class PlayLoader
{
    /// <summary>Supported <c>schemaVersion</c>.</summary>
    public const int SchemaVersion = 1;

    /// <summary>Parses and validates one play.</summary>
    /// <param name="json">File content.</param>
    /// <param name="fileId">File name without extension; must equal <c>id</c>.</param>
    /// <param name="rink">The rink (grid, goals, zones, faceoff spots).</param>
    /// <param name="plays">Play limits from tuning.json.</param>
    public static LoadResult<Play> Parse(string json, string fileId, Rink rink, PlaysConfig plays)
    {
        var errors = new List<string>();
        using JsonDocument? document = JsonDocuments.Parse(json, errors);
        if (document == null)
        {
            return LoadResult<Play>.Fail(errors);
        }

        Play? play = Map(JsonReader.Root(document.RootElement, errors), fileId);
        if (errors.Count > 0 || play == null)
        {
            return LoadResult<Play>.Fail(errors);
        }

        errors.AddRange(PlayValidator.Validate(play, rink, plays));
        return errors.Count > 0 ? LoadResult<Play>.Fail(errors) : LoadResult<Play>.Ok(play);
    }

    /// <summary>
    /// Loads every <c>*.json</c> in <paramref name="directory"/> in file-name order. Throws <see cref="DataLoadException"/>
    /// for the first invalid file. A missing directory gives an empty list.
    /// </summary>
    public static IReadOnlyList<Play> LoadAll(string directory, Rink rink, PlaysConfig plays)
    {
        var result = new List<Play>();
        foreach (string file in TacticsJson.JsonFiles(directory))
        {
            string fileId = Path.GetFileNameWithoutExtension(file);
            result.Add(Parse(File.ReadAllText(file), fileId, rink, plays).GetOrThrow("plays/" + Path.GetFileName(file)));
        }

        return result;
    }

    private static Play? Map(JsonReader root, string fileId)
    {
        if (!root.IsObject)
        {
            root.Error("expected an object, got " + root.Describe());
            return null;
        }

        root.RejectUnknown("schemaVersion", "id", "name", "type", "faceoffSpot", "start", "beats");
        JsonDocuments.RequireSchemaVersion(root, SchemaVersion);
        string? id = root.String("id");
        TacticsJson.RequireIdMatchesFile(root, id, fileId);
        string? name = root.String("name");
        PlayType? type = MapType(root.Required("type"));
        string? faceoffSpot = root.Optional("faceoffSpot")?.AsString();

        Position? carrier = null;
        var start = new List<KeyValuePair<Position, GridPoint>>();
        JsonReader? startReader = root.Required("start");
        if (startReader != null)
        {
            startReader.RejectUnknown("puckCarrier", "positions");
            carrier = TacticsJson.Position(startReader.Required("puckCarrier"));
            start = PositionNodes(startReader.Required("positions"));
        }

        var beats = new List<Beat>();
        JsonReader? beatsReader = root.Required("beats");
        bool beatsOk = true;
        if (beatsReader != null)
        {
            foreach (JsonReader beatReader in beatsReader.Items())
            {
                Beat? beat = MapBeat(beatReader);
                if (beat == null)
                {
                    beatsOk = false;
                }
                else
                {
                    beats.Add(beat);
                }
            }
        }

        if (id == null || name == null || !type.HasValue || !carrier.HasValue || startReader == null || beatsReader == null || !beatsOk)
        {
            return null;
        }

        return new Play(id, name, type.Value, faceoffSpot!, carrier.Value, start, beats);
    }

    private static PlayType? MapType(JsonReader? reader)
    {
        string? name = reader?.AsString();
        if (name == null)
        {
            return null;
        }

        if (!PlayNames.TryParseType(name, out PlayType type))
        {
            reader!.Error("unknown play type " + name + " (expected breakout, zoneEntry, offensiveZone, faceoff or powerPlay)");
            return null;
        }

        return type;
    }

    private static List<KeyValuePair<Position, GridPoint>> PositionNodes(JsonReader? reader)
    {
        var nodes = new List<KeyValuePair<Position, GridPoint>>();
        if (reader == null)
        {
            return nodes;
        }

        foreach (KeyValuePair<string, JsonReader> entry in reader.Properties())
        {
            Position? position = TacticsJson.PositionKey(entry.Key, entry.Value);
            GridPoint? node = TacticsJson.Pair(entry.Value);
            if (position.HasValue && node.HasValue)
            {
                nodes.Add(new KeyValuePair<Position, GridPoint>(position.Value, node.Value));
            }
        }

        return nodes;
    }

    private static Beat? MapBeat(JsonReader reader)
    {
        reader.RejectUnknown("moves", "action");
        JsonReader? movesReader = reader.Required("moves");
        List<KeyValuePair<Position, GridPoint>> moves = PositionNodes(movesReader);
        PlayAction? action = MapAction(reader.Required("action"));
        return movesReader != null && action != null ? new Beat(moves, action) : null;
    }

    private static PlayAction? MapAction(JsonReader? reader)
    {
        if (reader == null)
        {
            return null;
        }

        string? typeName = reader.String("type");
        if (typeName == null)
        {
            return null;
        }

        if (!PlayNames.TryParseAction(typeName, out PlayActionType type))
        {
            reader.Error("type", "unknown action type " + typeName + " (expected skate, pass, shoot, driveNet or dump)");
            return null;
        }

        switch (type)
        {
            case PlayActionType.Pass:
            {
                reader.RejectUnknown("type", "from", "to");
                Position? from = TacticsJson.Position(reader.Required("from"));
                Position? to = TacticsJson.Position(reader.Required("to"));
                return from.HasValue && to.HasValue ? PlayAction.Pass(from.Value, to.Value) : null;
            }

            case PlayActionType.Skate:
            case PlayActionType.Dump:
            {
                reader.RejectUnknown("type", "by", "to");
                Position? by = TacticsJson.Position(reader.Required("by"));
                GridPoint? to = TacticsJson.Pair(reader.Required("to"));
                if (!by.HasValue || !to.HasValue)
                {
                    return null;
                }

                return type == PlayActionType.Skate ? PlayAction.Skate(by.Value, to.Value) : PlayAction.Dump(by.Value, to.Value);
            }

            default:
            {
                reader.RejectUnknown("type", "by");
                Position? by = TacticsJson.Position(reader.Required("by"));
                if (!by.HasValue)
                {
                    return null;
                }

                return type == PlayActionType.Shoot ? PlayAction.Shoot(by.Value) : PlayAction.DriveNet(by.Value);
            }
        }
    }
}
