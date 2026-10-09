using System.Text.Json;

namespace HockeyCoach.Harness.Data;

/// <summary>One target metric (data/targets.json, D-016). Used by the Harness report only, never by Sim.</summary>
/// <param name="Name">Metric name, also used in the report.</param>
/// <param name="Unit">share, count, xg or seconds.</param>
/// <param name="Scope">perMatch, perTeamPerMatch, perTeam, perShot or perMatchup.</param>
/// <param name="Min">Inclusive lower bound, or null when not known yet.</param>
/// <param name="Max">Inclusive upper bound, or null when not known yet.</param>
/// <param name="Status">approved or placeholder.</param>
/// <param name="Source">Where the metric comes from.</param>
public sealed record TargetMetric(string Name, string Unit, string Scope, double? Min, double? Max, string Status, string Source);

/// <summary>All target metrics, in ordinal name order.</summary>
/// <param name="Metrics">Metrics sorted by name.</param>
public sealed record TargetsConfig(IReadOnlyList<TargetMetric> Metrics);

/// <summary>Loads data/targets.json (docs/data-schema.md, targets.json).</summary>
public static class TargetsLoader
{
    /// <summary>Supported <c>schemaVersion</c>.</summary>
    public const int SchemaVersion = 1;

    private static readonly string[] Units = { "share", "count", "xg", "seconds" };
    private static readonly string[] Scopes = { "perMatch", "perTeamPerMatch", "perTeam", "perShot", "perMatchup" };
    private static readonly string[] Statuses = { "approved", "placeholder" };

    /// <summary>Parses and validates targets JSON text.</summary>
    public static LoadResult<TargetsConfig> Parse(string json)
    {
        var errors = new List<string>();
        using JsonDocument? document = JsonDocuments.Parse(json, errors);
        if (document == null)
        {
            return LoadResult<TargetsConfig>.Fail(errors);
        }

        JsonReader root = JsonReader.Root(document.RootElement, errors);
        var metrics = new List<TargetMetric>();
        if (!root.IsObject)
        {
            root.Error("expected an object, got " + root.Describe());
            return LoadResult<TargetsConfig>.Fail(errors);
        }

        root.RejectUnknown("schemaVersion", "metrics");
        JsonDocuments.RequireSchemaVersion(root, SchemaVersion);
        JsonReader? metricsReader = root.Required("metrics");
        if (metricsReader != null)
        {
            foreach (KeyValuePair<string, JsonReader> metric in metricsReader.Properties())
            {
                TargetMetric? mapped = MapMetric(metric.Key, metric.Value);
                if (mapped != null)
                {
                    metrics.Add(mapped);
                }
            }
        }

        return errors.Count > 0 ? LoadResult<TargetsConfig>.Fail(errors) : LoadResult<TargetsConfig>.Ok(new TargetsConfig(metrics));
    }

    /// <summary>Reads and parses a targets file.</summary>
    public static LoadResult<TargetsConfig> Load(string path) => Parse(File.ReadAllText(path));

    private static TargetMetric? MapMetric(string name, JsonReader reader)
    {
        reader.RejectUnknown("unit", "scope", "min", "max", "status", "source");
        string? unit = OneOf(reader, "unit", Units);
        string? scope = OneOf(reader, "scope", Scopes);
        string? status = OneOf(reader, "status", Statuses);
        string? source = reader.String("source");
        double? min = reader.Required("min")?.AsNullableDouble();
        double? max = reader.Required("max")?.AsNullableDouble();

        if (min.HasValue && max.HasValue && min.Value > max.Value)
        {
            reader.Error("min (" + min.Value + ") is greater than max (" + max.Value + ")");
        }

        if (status == "approved" && (!min.HasValue || !max.HasValue))
        {
            reader.Error("approved metric needs both min and max");
        }

        if (unit == null || scope == null || status == null || source == null)
        {
            return null;
        }

        return new TargetMetric(name, unit, scope, min, max, status, source);
    }

    private static string? OneOf(JsonReader reader, string key, string[] allowed)
    {
        string? value = reader.String(key);
        if (value != null && Array.IndexOf(allowed, value) < 0)
        {
            reader.Error(key, "unknown value " + value + " (expected " + string.Join(", ", allowed) + ")");
            return null;
        }

        return value;
    }
}
