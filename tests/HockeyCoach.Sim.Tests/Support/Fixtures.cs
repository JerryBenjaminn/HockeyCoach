using System.Text.Json.Nodes;

namespace HockeyCoach.Sim.Tests.Support;

/// <summary>Access to JSON fixtures (copied to the output directory) and to the repository root.</summary>
internal static class Fixtures
{
    public static readonly string[] SmallRinkXgZones = { "slot", "longRange" };

    public static string Read(string name)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
    }

    /// <summary>Loads a fixture, applies <paramref name="edit"/> to its DOM and returns the JSON text.</summary>
    public static string Edit(string name, Action<JsonObject> edit)
    {
        JsonObject root = JsonNode.Parse(Read(name))!.AsObject();
        edit(root);
        return root.ToJsonString();
    }

    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HockeyCoach.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("HockeyCoach.sln not found above " + AppContext.BaseDirectory);
    }
}
