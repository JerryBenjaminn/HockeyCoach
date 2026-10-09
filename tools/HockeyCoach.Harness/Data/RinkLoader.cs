using System.Text.Json;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Harness.Data;

/// <summary>
/// Loads data/rink.json (docs/data-schema.md, rink.json): JSON → path-checked mapping → <see cref="Rink"/> →
/// <see cref="RinkValidator"/>.
/// </summary>
public static class RinkLoader
{
    /// <summary>Supported <c>schemaVersion</c>.</summary>
    public const int SchemaVersion = 1;

    /// <summary>Parses and validates rink JSON text.</summary>
    public static LoadResult<Rink> Parse(string json)
    {
        var errors = new List<string>();
        using JsonDocument? document = JsonDocuments.Parse(json, errors);
        if (document == null)
        {
            return LoadResult<Rink>.Fail(errors);
        }

        JsonReader root = JsonReader.Root(document.RootElement, errors);
        Rink? rink = Map(root);
        if (errors.Count > 0 || rink == null)
        {
            return LoadResult<Rink>.Fail(errors);
        }

        errors.AddRange(RinkValidator.Validate(rink));
        return errors.Count > 0 ? LoadResult<Rink>.Fail(errors) : LoadResult<Rink>.Ok(rink);
    }

    /// <summary>Reads and parses a rink file.</summary>
    public static LoadResult<Rink> Load(string path) => Parse(File.ReadAllText(path));

    private static Rink? Map(JsonReader root)
    {
        if (!root.IsObject)
        {
            root.Error("expected an object, got " + root.Describe());
            return null;
        }

        root.RejectUnknown("schemaVersion", "length", "width", "ownGoal", "opponentGoal", "zones", "xgZones", "faceoffSpots", "nodes");
        JsonDocuments.RequireSchemaVersion(root, SchemaVersion);
        int? length = Positive(root, "length");
        int? width = Positive(root, "width");
        GridPoint? ownGoal = Point(root.Required("ownGoal"));
        GridPoint? opponentGoal = Point(root.Required("opponentGoal"));
        List<ZoneRange> zones = Zones(root.Required("zones"));
        List<string> xgZones = Strings(root.Required("xgZones"));
        List<FaceoffSpot> spots = Spots(root.Required("faceoffSpots"));

        if (length == null || width == null)
        {
            return null;
        }

        List<RinkNode>? nodes = Nodes(root.Required("nodes"), length.Value, width.Value);
        if (nodes == null || ownGoal == null || opponentGoal == null)
        {
            return null;
        }

        return new Rink(length.Value, width.Value, nodes, zones, xgZones, ownGoal.Value, opponentGoal.Value, spots);
    }

    private static int? Positive(JsonReader root, string name)
    {
        int? value = root.Int(name);
        if (value.HasValue && value.Value <= 0)
        {
            root.Error(name, "must be positive, was " + value.Value);
            return null;
        }

        return value;
    }

    private static GridPoint? Point(JsonReader? reader)
    {
        if (reader == null)
        {
            return null;
        }

        reader.RejectUnknown("x", "y");
        int? x = reader.Int("x");
        int? y = reader.Int("y");
        return x.HasValue && y.HasValue ? new GridPoint(x.Value, y.Value) : null;
    }

    private static List<ZoneRange> Zones(JsonReader? reader)
    {
        var zones = new List<ZoneRange>();
        if (reader == null)
        {
            return zones;
        }

        foreach (JsonReader item in reader.Items())
        {
            item.RejectUnknown("id", "xMin", "xMax");
            string? id = item.String("id");
            int? xMin = item.Int("xMin");
            int? xMax = item.Int("xMax");
            RinkZone zone = default;
            if (id != null && !RinkZones.TryParse(id, out zone))
            {
                item.Error("id", "unknown zone " + id + " (expected defensive, neutral or offensive)");
                continue;
            }

            if (id != null && xMin.HasValue && xMax.HasValue)
            {
                zones.Add(new ZoneRange(zone, xMin.Value, xMax.Value));
            }
        }

        return zones;
    }

    private static List<string> Strings(JsonReader? reader)
    {
        var values = new List<string>();
        if (reader == null)
        {
            return values;
        }

        foreach (JsonReader item in reader.Items())
        {
            string? value = item.AsString();
            if (value != null)
            {
                values.Add(value);
            }
        }

        return values;
    }

    private static List<FaceoffSpot> Spots(JsonReader? reader)
    {
        var spots = new List<FaceoffSpot>();
        if (reader == null)
        {
            return spots;
        }

        foreach (JsonReader item in reader.Items())
        {
            item.RejectUnknown("id", "x", "y");
            string? id = item.String("id");
            int? x = item.Int("x");
            int? y = item.Int("y");
            if (id != null && x.HasValue && y.HasValue)
            {
                spots.Add(new FaceoffSpot(id, new GridPoint(x.Value, y.Value)));
            }
        }

        return spots;
    }

    private static List<RinkNode>? Nodes(JsonReader? reader, int length, int width)
    {
        if (reader == null)
        {
            return null;
        }

        IReadOnlyList<JsonReader> items = reader.Items();
        bool ok = true;
        if (items.Count != length * width)
        {
            reader.Error("expected " + (length * width) + " nodes (length x width), got " + items.Count);
            ok = false;
        }

        var nodes = new List<RinkNode>();
        for (int i = 0; i < items.Count; i++)
        {
            JsonReader item = items[i];
            item.RejectUnknown("x", "y", "zone", "xgZone", "isSlot");
            int? x = item.Int("x");
            int? y = item.Int("y");
            string? zoneName = item.String("zone");
            string? xgZone = item.String("xgZone");
            bool isSlot = item.Required("isSlot")?.AsBool() ?? false;
            RinkZone zone = default;
            if (zoneName != null && !RinkZones.TryParse(zoneName, out zone))
            {
                item.Error("zone", "unknown zone " + zoneName + " (expected defensive, neutral or offensive)");
                ok = false;
            }

            if (!x.HasValue || !y.HasValue || zoneName == null || xgZone == null)
            {
                ok = false;
                continue;
            }

            int expectedX = i / width;
            int expectedY = i % width;
            if (x.Value != expectedX || y.Value != expectedY)
            {
                item.Error("expected node (" + expectedX + "," + expectedY + ") in id order (id = x * width + y), got (" + x.Value + "," + y.Value + ")");
                ok = false;
                continue;
            }

            nodes.Add(new RinkNode(x.Value, y.Value, zone, xgZone, isSlot));
        }

        return ok ? nodes : null;
    }
}
