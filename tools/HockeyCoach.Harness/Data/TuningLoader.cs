using System.Text.Json;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Harness.Data;

/// <summary>
/// Loads data/tuning.json (docs/data-schema.md, tuning.json): JSON → path-checked mapping → <see cref="TuningConfig"/> →
/// <see cref="TuningValidator"/>. Sections for later milestones are accepted but not mapped yet.
/// </summary>
public static class TuningLoader
{
    /// <summary>Supported <c>schemaVersion</c>.</summary>
    public const int SchemaVersion = 1;

    /// <summary>Top-level sections mapped into <see cref="TuningConfig"/>.</summary>
    private static readonly string[] MappedSections =
    {
        "schemaVersion", "stats", "checkFormula", "checks", "positions", "time", "plays", "chanceTypes", "chanceClasses",
        "pressure", "loosePuckSpots",
    };

    /// <summary>Top-level sections of later milestones (data-schema.md); accepted and not mapped yet.</summary>
    private static readonly string[] LaterSections =
    {
        "energy", "organization", "form", "chemistry", "familiarity",
    };

    /// <summary>Team pressure state keys of <c>pressure</c> (milestone 3): accepted, not mapped yet.</summary>
    private static readonly string[] LaterPressureKeys =
    {
        "gainOnZoneEntry", "gainOnShot", "gainPerSecondInZone", "keepOnStoppage", "energyDrainPerSecond", "mentalToughnessReductionPerPoint",
    };

    private static readonly string[] CheckKeys = { "kind", "side", "p0", "attacker", "defender", "modifiers" };

    /// <summary>Parses and validates tuning JSON text against the rink's xG zones.</summary>
    /// <param name="json">tuning.json content.</param>
    /// <param name="xgZones">rink.json <c>xgZones</c>; <c>baseXg</c> and <c>attackerByXgZone</c> must cover exactly these.</param>
    public static LoadResult<TuningConfig> Parse(string json, IReadOnlyList<string> xgZones)
    {
        var errors = new List<string>();
        using JsonDocument? document = JsonDocuments.Parse(json, errors);
        if (document == null)
        {
            return LoadResult<TuningConfig>.Fail(errors);
        }

        TuningConfig? tuning = Map(JsonReader.Root(document.RootElement, errors));
        if (errors.Count > 0 || tuning == null)
        {
            return LoadResult<TuningConfig>.Fail(errors);
        }

        errors.AddRange(TuningValidator.Validate(tuning, xgZones));
        return errors.Count > 0 ? LoadResult<TuningConfig>.Fail(errors) : LoadResult<TuningConfig>.Ok(tuning);
    }

    /// <summary>Reads and parses a tuning file.</summary>
    public static LoadResult<TuningConfig> Load(string path, IReadOnlyList<string> xgZones) => Parse(File.ReadAllText(path), xgZones);

    private static TuningConfig? Map(JsonReader root)
    {
        if (!root.IsObject)
        {
            root.Error("expected an object, got " + root.Describe());
            return null;
        }

        root.RejectUnknown(MappedSections.Concat(LaterSections).ToArray());
        JsonDocuments.RequireSchemaVersion(root, SchemaVersion);

        StatsConfig? stats = MapStats(root.Required("stats"));
        CheckFormulaConfig? formula = MapFormula(root.Required("checkFormula"));
        PositionsConfig? positions = MapPositions(root.Required("positions"));
        TimeConfig? time = MapTime(root.Required("time"));
        PlaysConfig? plays = MapPlays(root.Required("plays"));
        ChanceTypesConfig? chanceTypes = MapChanceTypes(root.Required("chanceTypes"));
        ChanceClassesConfig? chanceClasses = MapChanceClasses(root.Required("chanceClasses"));
        PressureConfig? pressure = MapPressure(root.Required("pressure"));
        LoosePuckSpotsConfig? loosePuckSpots = MapLoosePuckSpots(root.Required("loosePuckSpots"));

        var checks = new List<CheckDefinition>();
        ShotConfig? shot = null;
        JsonReader? checksReader = root.Required("checks");
        if (checksReader != null)
        {
            foreach (KeyValuePair<string, JsonReader> check in checksReader.Properties())
            {
                if (check.Key == ShotConfig.Id)
                {
                    shot = MapShot(check.Value);
                }
                else
                {
                    CheckDefinition? definition = MapCheck(check.Key, check.Value);
                    if (definition != null)
                    {
                        checks.Add(definition);
                    }
                }
            }

            if (checksReader.IsObject && checksReader.Optional(ShotConfig.Id) == null)
            {
                checksReader.Error(ShotConfig.Id, "missing");
            }
        }

        if (stats == null || formula == null || positions == null || shot == null || time == null || plays == null
            || chanceTypes == null || chanceClasses == null || pressure == null || loosePuckSpots == null)
        {
            return null;
        }

        return new TuningConfig(stats, formula, checks, shot, positions, time, plays, chanceTypes, chanceClasses, pressure, loosePuckSpots);
    }

    private static PressureConfig? MapPressure(JsonReader? reader)
    {
        if (reader == null)
        {
            return null;
        }

        reader.RejectUnknown(LaterPressureKeys.Append("underPressureNodes").ToArray());
        int? nodes = reader.Int("underPressureNodes");
        return nodes.HasValue ? new PressureConfig(nodes.Value) : null;
    }

    private static LoosePuckSpotsConfig? MapLoosePuckSpots(JsonReader? reader)
    {
        if (reader == null)
        {
            return null;
        }

        reader.RejectUnknown(LoosePuckSpotsConfig.Keys.ToArray());
        var rules = new Dictionary<string, LoosePuckRule>(StringComparer.Ordinal);
        foreach (string key in LoosePuckSpotsConfig.Keys)
        {
            string? name = reader.String(key);
            if (name == null)
            {
                continue;
            }

            if (!LoosePuckSpotsConfig.TryParseRule(name, out LoosePuckRule rule))
            {
                reader.Error(key, "unknown rule " + name + " (expected netFront, shooterSideCorner, endRowShooterLane, blockerNode or laneDefenderNode)");
                continue;
            }

            rules[key] = rule;
        }

        if (rules.Count != LoosePuckSpotsConfig.Keys.Count)
        {
            return null;
        }

        return new LoosePuckSpotsConfig(rules["reboundSlot"], rules["reboundCorner"], rules["missedShot"], rules["blockedShot"], rules["failedPass"]);
    }

    private static TimeConfig? MapTime(JsonReader? reader)
    {
        if (reader == null)
        {
            return null;
        }

        reader.RejectUnknown(
            "periods", "periodSeconds", "forwardShiftSeconds", "defenceShiftSeconds", "secondsPerAction", "setupSeconds", "regroupSeconds");
        int? periods = reader.Int("periods");
        var secondsPerAction = new List<KeyValuePair<string, double>>();
        JsonReader? actions = reader.Required("secondsPerAction");
        if (actions != null)
        {
            foreach (KeyValuePair<string, JsonReader> action in actions.Properties())
            {
                secondsPerAction.Add(new KeyValuePair<string, double>(action.Key, action.Value.AsDouble()));
            }
        }

        var time = new TimeConfig(
            periods ?? 0,
            reader.Double("periodSeconds"),
            reader.Double("forwardShiftSeconds"),
            reader.Double("defenceShiftSeconds"),
            secondsPerAction,
            reader.Double("setupSeconds"),
            reader.Double("regroupSeconds"));
        return periods.HasValue ? time : null;
    }

    private static PlaysConfig? MapPlays(JsonReader? reader)
    {
        if (reader == null)
        {
            return null;
        }

        reader.RejectUnknown("maxBeats", "maxNodesPerBeat");
        int? maxBeats = reader.Int("maxBeats");
        int? maxNodesPerBeat = reader.Int("maxNodesPerBeat");
        return maxBeats.HasValue && maxNodesPerBeat.HasValue ? new PlaysConfig(maxBeats.Value, maxNodesPerBeat.Value) : null;
    }

    private static ChanceTypesConfig? MapChanceTypes(JsonReader? reader)
    {
        if (reader == null)
        {
            return null;
        }

        reader.RejectUnknown("rushOrganizationBelow", "turnoverWindowSeconds");
        return new ChanceTypesConfig(reader.Double("rushOrganizationBelow"), reader.Double("turnoverWindowSeconds"));
    }

    private static ChanceClassesConfig? MapChanceClasses(JsonReader? reader)
    {
        if (reader == null)
        {
            return null;
        }

        reader.RejectUnknown("topMinXg", "goodMinXg", "moderateMinXg");
        return new ChanceClassesConfig(reader.Double("topMinXg"), reader.Double("goodMinXg"), reader.Double("moderateMinXg"));
    }

    private static StatsConfig? MapStats(JsonReader? reader)
    {
        if (reader == null)
        {
            return null;
        }

        reader.RejectUnknown("min", "max", "grades");
        int? min = reader.Int("min");
        int? max = reader.Int("max");
        var grades = new List<StatGrade>();
        JsonReader? gradesReader = reader.Required("grades");
        if (gradesReader != null)
        {
            foreach (KeyValuePair<string, JsonReader> grade in gradesReader.Properties())
            {
                IReadOnlyList<JsonReader> bounds = grade.Value.Items();
                if (bounds.Count != 2)
                {
                    grade.Value.Error("expected [min, max]");
                    continue;
                }

                int? low = bounds[0].AsInt();
                int? high = bounds[1].AsInt();
                if (low.HasValue && high.HasValue)
                {
                    grades.Add(new StatGrade(grade.Key, low.Value, high.Value));
                }
            }
        }

        return min.HasValue && max.HasValue ? new StatsConfig(min.Value, max.Value, grades) : null;
    }

    private static CheckFormulaConfig? MapFormula(JsonReader? reader)
    {
        if (reader == null)
        {
            return null;
        }

        reader.RejectUnknown("k", "minProbability", "maxProbability", "referenceValue");
        return new CheckFormulaConfig(
            reader.Double("k"),
            reader.Double("minProbability"),
            reader.Double("maxProbability"),
            reader.Double("referenceValue"));
    }

    private static PositionsConfig? MapPositions(JsonReader? reader)
    {
        if (reader == null)
        {
            return null;
        }

        reader.RejectUnknown("offSideCheckModifier");
        return new PositionsConfig(reader.Double("offSideCheckModifier"));
    }

    private static CheckDefinition? MapCheck(string id, JsonReader reader)
    {
        if (!reader.IsObject)
        {
            reader.Error("expected an object, got " + reader.Describe());
            return null;
        }

        CheckKind? kind = MapKind(reader);
        CheckSide? side = null;
        JsonReader? sideReader = reader.Optional("side");
        if (sideReader != null)
        {
            side = MapSide(sideReader);
        }

        if (kind == CheckKind.OneSided && sideReader == null)
        {
            reader.Error("side", "missing (oneSided check needs side attacker or defender)");
        }
        else if (kind.HasValue && kind != CheckKind.OneSided && sideReader != null)
        {
            sideReader.Error("only oneSided checks have a side");
        }

        double? p0 = reader.Optional("p0")?.AsDouble();
        if (kind is CheckKind.TwoSided or CheckKind.OneSided && p0 == null)
        {
            reader.Error("p0", "missing");
        }

        IReadOnlyList<WeightTerm> attacker = MapSideTerms(reader.Optional("attacker"));
        IReadOnlyList<WeightTerm> defender = MapSideTerms(reader.Optional("defender"));
        CheckSideKeys(reader, kind, sideReader == null ? null : side);

        CheckModifiers modifiers = MapModifiers(reader.Optional("modifiers"));
        Dictionary<string, double> parameters = MapParameters(reader, CheckKeys);

        if (!kind.HasValue)
        {
            return null;
        }

        return new CheckDefinition(id, kind.Value, sideReader == null ? null : side, p0, attacker, defender, modifiers, parameters);
    }

    /// <summary>Reports sides that must be absent or present for the kind (data-schema.md, Validointi).</summary>
    private static void CheckSideKeys(JsonReader reader, CheckKind? kind, CheckSide? side)
    {
        bool hasAttacker = reader.Optional("attacker") != null;
        bool hasDefender = reader.Optional("defender") != null;
        switch (kind)
        {
            case CheckKind.TwoSided:
                if (!hasAttacker)
                {
                    reader.Error("attacker", "missing (twoSided check)");
                }

                if (!hasDefender)
                {
                    reader.Error("defender", "missing (twoSided check)");
                }

                break;

            case CheckKind.OneSided when side.HasValue:
                bool attackerPresent = side.Value == CheckSide.Attacker;
                if (!(attackerPresent ? hasAttacker : hasDefender))
                {
                    reader.Error(attackerPresent ? "attacker" : "defender", "missing (oneSided check with side " + (attackerPresent ? "attacker" : "defender") + ")");
                }

                if (attackerPresent ? hasDefender : hasAttacker)
                {
                    reader.Error(attackerPresent ? "defender" : "attacker", "must not be written in a oneSided check with side " + (attackerPresent ? "attacker" : "defender"));
                }

                break;

            case CheckKind.NoCheck:
                foreach (string key in new[] { "p0", "attacker", "defender" })
                {
                    if (reader.Optional(key) != null)
                    {
                        reader.Error(key, "noCheck must not have " + key);
                    }
                }

                break;
        }
    }

    private static ShotConfig? MapShot(JsonReader reader)
    {
        if (!reader.IsObject)
        {
            reader.Error("expected an object, got " + reader.Describe());
            return null;
        }

        CheckKind? kind = MapKind(reader);
        if (kind.HasValue && kind.Value != CheckKind.TwoSided)
        {
            reader.Error("kind", "shot must be twoSided");
        }

        foreach (string forbidden in new[] { "p0", "attacker", "side" })
        {
            JsonReader? present = reader.Optional(forbidden);
            present?.Error("shot has no " + forbidden + " (p0 = baseXg[xgZone], attacker = attackerByXgZone[xgZone])");
        }

        var attackerByXgZone = new List<KeyValuePair<string, IReadOnlyList<WeightTerm>>>();
        JsonReader? byZone = reader.Required("attackerByXgZone");
        if (byZone != null)
        {
            foreach (KeyValuePair<string, JsonReader> zone in byZone.Properties())
            {
                attackerByXgZone.Add(new KeyValuePair<string, IReadOnlyList<WeightTerm>>(zone.Key, MapSideTerms(zone.Value)));
            }
        }

        IReadOnlyList<WeightTerm> defender = MapSideTerms(reader.Required("defender"));
        var baseXg = new List<KeyValuePair<string, double>>();
        JsonReader? baseXgReader = reader.Required("baseXg");
        if (baseXgReader != null)
        {
            foreach (KeyValuePair<string, JsonReader> zone in baseXgReader.Properties())
            {
                baseXg.Add(new KeyValuePair<string, double>(zone.Key, zone.Value.AsDouble()));
            }
        }

        double onTargetShare = reader.Double("onTargetShare");
        double minProbability = reader.Double("minProbability");
        double maxProbability = reader.Double("maxProbability");
        CheckModifiers modifiers = MapModifiers(reader.Optional("modifiers"));
        Dictionary<string, double> parameters = MapParameters(
            reader,
            new[] { "kind", "p0", "attacker", "side", "attackerByXgZone", "defender", "baseXg", "onTargetShare", "minProbability", "maxProbability", "modifiers" });

        return new ShotConfig(attackerByXgZone, defender, baseXg, onTargetShare, minProbability, maxProbability, modifiers, parameters);
    }

    private static CheckKind? MapKind(JsonReader reader)
    {
        string? kind = reader.String("kind");
        switch (kind)
        {
            case null: return null;
            case "twoSided": return CheckKind.TwoSided;
            case "oneSided": return CheckKind.OneSided;
            case "noCheck": return CheckKind.NoCheck;
            default:
                reader.Error("kind", "unknown kind " + kind + " (expected twoSided, oneSided or noCheck)");
                return null;
        }
    }

    private static CheckSide? MapSide(JsonReader reader)
    {
        string? side = reader.AsString();
        switch (side)
        {
            case null: return null;
            case "attacker": return CheckSide.Attacker;
            case "defender": return CheckSide.Defender;
            default:
                reader.Error("unknown side " + side + " (expected attacker or defender)");
                return null;
        }
    }

    /// <summary>Side object: role → stat → weight. Terms come out in ordinal (role, stat) order.</summary>
    private static IReadOnlyList<WeightTerm> MapSideTerms(JsonReader? reader)
    {
        var terms = new List<WeightTerm>();
        if (reader == null)
        {
            return terms;
        }

        foreach (KeyValuePair<string, JsonReader> role in reader.Properties())
        {
            if (!CheckRoles.IsKnown(role.Key))
            {
                role.Value.Error("unknown participant role (known: " + string.Join(", ", CheckRoles.All()) + ")");
                continue;
            }

            StatOwner owner = CheckRoles.StatOwnerOf(role.Key);
            foreach (KeyValuePair<string, JsonReader> stat in role.Value.Properties())
            {
                double weight = stat.Value.AsDouble();
                if (!StatNames.TryParse(owner, stat.Key, out StatRef statRef))
                {
                    stat.Value.Error("unknown " + (owner == StatOwner.Goalie ? "goalie" : "skater") + " stat");
                    continue;
                }

                terms.Add(new WeightTerm(role.Key, statRef, weight));
            }
        }

        return terms;
    }

    private static CheckModifiers MapModifiers(JsonReader? reader)
    {
        if (reader == null)
        {
            return CheckModifiers.None;
        }

        var scalars = new List<KeyValuePair<string, double>>();
        var tables = new List<KeyValuePair<string, double[]>>();
        foreach (KeyValuePair<string, JsonReader> modifier in reader.Properties())
        {
            if (modifier.Value.Element.ValueKind == JsonValueKind.Array)
            {
                tables.Add(new KeyValuePair<string, double[]>(modifier.Key, modifier.Value.Items().Select(i => i.AsDouble()).ToArray()));
            }
            else
            {
                scalars.Add(new KeyValuePair<string, double>(modifier.Key, modifier.Value.AsDouble()));
            }
        }

        return new CheckModifiers(scalars, tables);
    }

    /// <summary>Remaining keys of a check: numbers become parameters, anything else is an error.</summary>
    private static Dictionary<string, double> MapParameters(JsonReader reader, string[] knownKeys)
    {
        var parameters = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, JsonReader> property in reader.Properties())
        {
            if (Array.IndexOf(knownKeys, property.Key) >= 0)
            {
                continue;
            }

            if (property.Value.Element.ValueKind != JsonValueKind.Number)
            {
                property.Value.Error("unknown key (extra check fields must be numbers)");
                continue;
            }

            parameters[property.Key] = property.Value.AsDouble();
        }

        return parameters;
    }
}
