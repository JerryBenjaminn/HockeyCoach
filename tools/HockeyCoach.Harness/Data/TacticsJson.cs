using HockeyCoach.Sim.Model;

namespace HockeyCoach.Harness.Data;

/// <summary>Shared readers for plays and systems: <c>[x, y]</c> coordinates, positions and file listing.</summary>
internal static class TacticsJson
{
    /// <summary>Reads <c>[a, b]</c> (two integers); records an error and returns null otherwise.</summary>
    public static GridPoint? Pair(JsonReader? reader)
    {
        if (reader == null)
        {
            return null;
        }

        IReadOnlyList<JsonReader> items = reader.Items();
        if (items.Count != 2)
        {
            if (reader.Element.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                reader.Error("expected [x, y], got " + items.Count + " values");
            }

            return null;
        }

        int? x = items[0].AsInt();
        int? y = items[1].AsInt();
        return x.HasValue && y.HasValue ? new GridPoint(x.Value, y.Value) : null;
    }

    /// <summary>Reads a position name (LW, C, RW, LD, RD).</summary>
    public static Position? Position(JsonReader? reader)
    {
        string? name = reader?.AsString();
        if (name == null)
        {
            return null;
        }

        if (!Positions.TryParse(name, out Position position))
        {
            reader!.Error("unknown position " + name + " (expected LW, C, RW, LD or RD)");
            return null;
        }

        return position;
    }

    /// <summary>Parses a position used as an object key, reporting it at <paramref name="reader"/>.</summary>
    public static Position? PositionKey(string key, JsonReader reader)
    {
        if (!Positions.TryParse(key, out Position position))
        {
            reader.Error("unknown position " + key + " (expected LW, C, RW, LD or RD)");
            return null;
        }

        return position;
    }

    /// <summary>The <c>*.json</c> files of a directory in ordinal file-name order; empty if the directory is missing.</summary>
    public static IReadOnlyList<string> JsonFiles(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return Array.Empty<string>();
        }

        string[] files = Directory.GetFiles(directory, "*.json");
        Array.Sort(files, (a, b) => string.CompareOrdinal(Path.GetFileName(a), Path.GetFileName(b)));
        return files;
    }

    /// <summary>Checks that <c>id</c> equals the file name (without extension).</summary>
    public static void RequireIdMatchesFile(JsonReader root, string? id, string fileId)
    {
        if (id != null && !string.Equals(id, fileId, StringComparison.Ordinal))
        {
            root.Error("id", "id " + id + " does not match the file name " + fileId);
        }
    }
}
