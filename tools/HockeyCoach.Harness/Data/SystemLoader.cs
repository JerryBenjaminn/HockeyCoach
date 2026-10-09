using System.Text.Json;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Harness.Data;

/// <summary>
/// Loads data/systems/*.json (docs/data-schema.md, Puolustusjärjestelmät): JSON → path-checked mapping →
/// <see cref="DefensiveSystem"/> → <see cref="SystemValidator"/>. One system per file; the id must equal the file name.
/// </summary>
public static class SystemLoader
{
    /// <summary>Supported <c>schemaVersion</c>.</summary>
    public const int SchemaVersion = 1;

    /// <summary>Parses and validates one system.</summary>
    /// <param name="json">File content.</param>
    /// <param name="fileId">File name without extension; must equal <c>id</c>.</param>
    /// <param name="rink">The rink.</param>
    public static LoadResult<DefensiveSystem> Parse(string json, string fileId, Rink rink)
    {
        var errors = new List<string>();
        using JsonDocument? document = JsonDocuments.Parse(json, errors);
        if (document == null)
        {
            return LoadResult<DefensiveSystem>.Fail(errors);
        }

        DefensiveSystem? system = Map(JsonReader.Root(document.RootElement, errors), fileId);
        if (errors.Count > 0 || system == null)
        {
            return LoadResult<DefensiveSystem>.Fail(errors);
        }

        errors.AddRange(SystemValidator.Validate(system, rink));
        return errors.Count > 0 ? LoadResult<DefensiveSystem>.Fail(errors) : LoadResult<DefensiveSystem>.Ok(system);
    }

    /// <summary>
    /// Loads every <c>*.json</c> in <paramref name="directory"/> in file-name order. Throws <see cref="DataLoadException"/>
    /// for the first invalid file. A missing directory gives an empty list.
    /// </summary>
    public static IReadOnlyList<DefensiveSystem> LoadAll(string directory, Rink rink)
    {
        var result = new List<DefensiveSystem>();
        foreach (string file in TacticsJson.JsonFiles(directory))
        {
            string fileId = Path.GetFileNameWithoutExtension(file);
            result.Add(Parse(File.ReadAllText(file), fileId, rink).GetOrThrow("systems/" + Path.GetFileName(file)));
        }

        return result;
    }

    private static DefensiveSystem? Map(JsonReader root, string fileId)
    {
        if (!root.IsObject)
        {
            root.Error("expected an object, got " + root.Describe());
            return null;
        }

        root.RejectUnknown("schemaVersion", "id", "name", "mirrorY", "rules");
        JsonDocuments.RequireSchemaVersion(root, SchemaVersion);
        string? id = root.String("id");
        TacticsJson.RequireIdMatchesFile(root, id, fileId);
        string? name = root.String("name");
        bool mirrorY = root.Required("mirrorY")?.AsBool() ?? false;

        var rules = new List<SystemRule>();
        bool rulesOk = true;
        JsonReader? rulesReader = root.Required("rules");
        if (rulesReader != null)
        {
            foreach (JsonReader ruleReader in rulesReader.Items())
            {
                SystemRule? rule = MapRule(ruleReader);
                if (rule == null)
                {
                    rulesOk = false;
                }
                else
                {
                    rules.Add(rule);
                }
            }
        }

        if (id == null || name == null || rulesReader == null || !rulesOk)
        {
            return null;
        }

        return new DefensiveSystem(id, name, mirrorY, rules);
    }

    private static SystemRule? MapRule(JsonReader reader)
    {
        reader.RejectUnknown("when", "targets");
        SystemCondition? when = MapCondition(reader.Required("when"));
        var targets = new List<KeyValuePair<SystemRole, SystemTarget>>();
        bool targetsOk = true;
        JsonReader? targetsReader = reader.Required("targets");
        if (targetsReader != null)
        {
            foreach (KeyValuePair<string, JsonReader> entry in targetsReader.Properties())
            {
                if (!SystemNames.TryParseRole(entry.Key, out SystemRole role))
                {
                    entry.Value.Error("unknown role " + entry.Key + " (expected F1, F2, F3, D1 or D2)");
                    targetsOk = false;
                    continue;
                }

                SystemTarget? target = MapTarget(entry.Value);
                if (target == null)
                {
                    targetsOk = false;
                    continue;
                }

                targets.Add(new KeyValuePair<SystemRole, SystemTarget>(role, target));
            }
        }

        return when != null && targetsReader != null && targetsOk ? new SystemRule(when, targets) : null;
    }

    private static SystemCondition? MapCondition(JsonReader? reader)
    {
        if (reader == null)
        {
            return null;
        }

        reader.RejectUnknown("puckZones", "puckX", "puckY", "puckState");
        List<RinkZone>? zones = null;
        JsonReader? zonesReader = reader.Optional("puckZones");
        bool ok = true;
        if (zonesReader != null)
        {
            zones = new List<RinkZone>();
            foreach (JsonReader item in zonesReader.Items())
            {
                string? zoneName = item.AsString();
                if (zoneName == null)
                {
                    ok = false;
                }
                else if (RinkZones.TryParse(zoneName, out RinkZone zone))
                {
                    zones.Add(zone);
                }
                else
                {
                    item.Error("unknown zone " + zoneName + " (expected defensive, neutral or offensive)");
                    ok = false;
                }
            }
        }

        IntRange? puckX = Range(reader.Optional("puckX"), ref ok);
        IntRange? puckY = Range(reader.Optional("puckY"), ref ok);
        PuckState? state = null;
        string? stateName = reader.Optional("puckState")?.AsString();
        if (stateName != null)
        {
            if (SystemNames.TryParsePuckState(stateName, out PuckState parsed))
            {
                state = parsed;
            }
            else
            {
                reader.Error("puckState", "unknown puck state " + stateName + " (expected controlled or loose)");
                ok = false;
            }
        }

        return ok ? new SystemCondition(zones, puckX!, puckY!, state) : null;
    }

    private static IntRange? Range(JsonReader? reader, ref bool ok)
    {
        if (reader == null)
        {
            return null;
        }

        GridPoint? pair = TacticsJson.Pair(reader);
        if (pair == null)
        {
            ok = false;
            return null;
        }

        return new IntRange(pair.Value.X, pair.Value.Y);
    }

    private static SystemTarget? MapTarget(JsonReader reader)
    {
        reader.RejectUnknown("node", "puckOffset");
        JsonReader? node = reader.Optional("node");
        JsonReader? offset = reader.Optional("puckOffset");
        if ((node == null) == (offset == null))
        {
            reader.Error("target must have exactly one of node and puckOffset");
            return null;
        }

        GridPoint? value = TacticsJson.Pair(node ?? offset);
        if (value == null)
        {
            return null;
        }

        return node != null ? SystemTarget.Node(value.Value) : SystemTarget.PuckOffset(value.Value.X, value.Value.Y);
    }
}
