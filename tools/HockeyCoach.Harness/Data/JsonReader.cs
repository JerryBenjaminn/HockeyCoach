using System.Globalization;
using System.Text.Json;

namespace HockeyCoach.Harness.Data;

/// <summary>
/// Path-tracking reader over a <see cref="JsonElement"/>. Every problem is recorded as
/// "<c>path: message</c>" (dot path as in tuning.json <c>_placeholders</c>, list indexes as <c>[i]</c>)
/// instead of throwing, so a loader can report all errors of a file at once.
/// Keys starting with '_' are ignored on every level (docs/data-schema.md).
/// </summary>
internal sealed class JsonReader
{
    private readonly List<string> _errors;

    private JsonReader(JsonElement element, string path, List<string> errors)
    {
        Element = element;
        Path = path;
        _errors = errors;
    }

    /// <summary>The wrapped element.</summary>
    public JsonElement Element { get; }

    /// <summary>Data path of the element; empty for the root.</summary>
    public string Path { get; }

    /// <summary>Creates a root reader writing into <paramref name="errors"/>.</summary>
    public static JsonReader Root(JsonElement element, List<string> errors) => new(element, string.Empty, errors);

    /// <summary>Whether the element is a JSON object.</summary>
    public bool IsObject => Element.ValueKind == JsonValueKind.Object;

    /// <summary>Records an error at this element's path.</summary>
    public void Error(string message) => _errors.Add((Path.Length == 0 ? "(root)" : Path) + ": " + message);

    /// <summary>Records an error at a child path.</summary>
    public void Error(string child, string message) => _errors.Add(Combine(Path, child) + ": " + message);

    /// <summary>
    /// Non-meta properties in ordinal key order. Duplicate keys are reported. Returns nothing if this is not an object
    /// (and records an error).
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, JsonReader>> Properties()
    {
        if (!RequireKind(JsonValueKind.Object, "an object"))
        {
            return Array.Empty<KeyValuePair<string, JsonReader>>();
        }

        var result = new SortedDictionary<string, JsonReader>(StringComparer.Ordinal);
        foreach (JsonProperty property in Element.EnumerateObject())
        {
            if (property.Name.StartsWith('_'))
            {
                continue;
            }

            if (result.ContainsKey(property.Name))
            {
                Error(property.Name, "duplicate key");
                continue;
            }

            result.Add(property.Name, new JsonReader(property.Value, Combine(Path, property.Name), _errors));
        }

        return result.ToList();
    }

    /// <summary>Returns the child property, or null if it is absent (not an error).</summary>
    public JsonReader? Optional(string name)
    {
        if (Element.ValueKind != JsonValueKind.Object || !Element.TryGetProperty(name, out JsonElement child))
        {
            return null;
        }

        return new JsonReader(child, Combine(Path, name), _errors);
    }

    /// <summary>Returns the child property, or records "missing" and returns null.</summary>
    public JsonReader? Required(string name)
    {
        if (!RequireKind(JsonValueKind.Object, "an object"))
        {
            return null;
        }

        JsonReader? child = Optional(name);
        if (child == null)
        {
            Error(name, "missing");
        }

        return child;
    }

    /// <summary>Reports every non-meta key that is not in <paramref name="known"/>.</summary>
    public void RejectUnknown(params string[] known)
    {
        foreach (KeyValuePair<string, JsonReader> property in Properties())
        {
            if (Array.IndexOf(known, property.Key) < 0)
            {
                property.Value.Error("unknown key");
            }
        }
    }

    /// <summary>List items; records an error if this is not an array.</summary>
    public IReadOnlyList<JsonReader> Items()
    {
        if (!RequireKind(JsonValueKind.Array, "an array"))
        {
            return Array.Empty<JsonReader>();
        }

        var items = new List<JsonReader>();
        int index = 0;
        foreach (JsonElement item in Element.EnumerateArray())
        {
            items.Add(new JsonReader(item, Path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]", _errors));
            index++;
        }

        return items;
    }

    /// <summary>The value as a double; records an error and returns NaN if it is not a number.</summary>
    public double AsDouble()
    {
        if (Element.ValueKind == JsonValueKind.Number && Element.TryGetDouble(out double value))
        {
            return value;
        }

        Error("expected a number, got " + Describe());
        return double.NaN;
    }

    /// <summary>The value as an int; records an error and returns null if it is not an integer.</summary>
    public int? AsInt()
    {
        if (Element.ValueKind == JsonValueKind.Number && Element.TryGetInt32(out int value))
        {
            return value;
        }

        Error("expected an integer, got " + Describe());
        return null;
    }

    /// <summary>The value as a string; records an error and returns null if it is not a string.</summary>
    public string? AsString()
    {
        if (Element.ValueKind == JsonValueKind.String)
        {
            return Element.GetString();
        }

        Error("expected a string, got " + Describe());
        return null;
    }

    /// <summary>The value as a bool; records an error and returns false if it is not a boolean.</summary>
    public bool AsBool()
    {
        if (Element.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return Element.GetBoolean();
        }

        Error("expected true or false, got " + Describe());
        return false;
    }

    /// <summary>The value as a number or null (JSON null); records an error for anything else.</summary>
    public double? AsNullableDouble()
    {
        return Element.ValueKind == JsonValueKind.Null ? null : AsDouble();
    }

    /// <summary>Required double child.</summary>
    public double Double(string name) => Required(name)?.AsDouble() ?? double.NaN;

    /// <summary>Required int child.</summary>
    public int? Int(string name) => Required(name)?.AsInt();

    /// <summary>Required string child.</summary>
    public string? String(string name) => Required(name)?.AsString();

    /// <summary>Short description of the element's kind for error messages.</summary>
    public string Describe()
    {
        return Element.ValueKind switch
        {
            JsonValueKind.Object => "an object",
            JsonValueKind.Array => "an array",
            JsonValueKind.String => "a string",
            JsonValueKind.Number => "a number",
            JsonValueKind.True or JsonValueKind.False => "a boolean",
            JsonValueKind.Null => "null",
            _ => "nothing",
        };
    }

    private bool RequireKind(JsonValueKind kind, string description)
    {
        if (Element.ValueKind == kind)
        {
            return true;
        }

        Error("expected " + description + ", got " + Describe());
        return false;
    }

    private static string Combine(string path, string child) => path.Length == 0 ? child : path + "." + child;
}
