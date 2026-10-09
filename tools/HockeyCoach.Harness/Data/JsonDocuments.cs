using System.Text.Json;

namespace HockeyCoach.Harness.Data;

/// <summary>Shared parsing entry for the data loaders.</summary>
internal static class JsonDocuments
{
    /// <summary>Strict options: no comments, no trailing commas (data-schema.md: plain JSON).</summary>
    private static readonly JsonDocumentOptions Options = new()
    {
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
    };

    /// <summary>Parses <paramref name="json"/>; on syntax errors records one error and returns null.</summary>
    public static JsonDocument? Parse(string json, List<string> errors)
    {
        try
        {
            return JsonDocument.Parse(json, Options);
        }
        catch (JsonException ex)
        {
            errors.Add("(root): invalid JSON: " + ex.Message);
            return null;
        }
    }

    /// <summary>Checks <c>schemaVersion</c> against the supported version.</summary>
    public static void RequireSchemaVersion(JsonReader root, int supported)
    {
        int? version = root.Int("schemaVersion");
        if (version.HasValue && version.Value != supported)
        {
            root.Error("schemaVersion", "unsupported version " + version.Value + ", expected " + supported);
        }
    }
}
