using System.Text.Json.Nodes;
using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Support;

/// <summary>The repository's data/tuning.json, edited in memory for tests.</summary>
internal static class RepositoryTuning
{
    private static string DataDirectory => Path.Combine(Fixtures.RepoRoot(), "data");

    /// <summary>The repository tuning.json as a DOM.</summary>
    public static JsonObject Read() => JsonNode.Parse(File.ReadAllText(Path.Combine(DataDirectory, "tuning.json")))!.AsObject();

    /// <summary>Parses the repository tuning after <paramref name="edit"/> against the repository rink's xG zones.</summary>
    public static LoadResult<TuningConfig> Parse(Action<JsonObject> edit)
    {
        JsonObject root = Read();
        edit(root);
        return TuningLoader.Parse(root.ToJsonString(), ShiftTestData.Data.Rink.XgZones);
    }

    /// <summary>Loads the whole repository data with the edited tuning (throws <see cref="DataLoadException"/> when invalid).</summary>
    public static GameData Load(Action<JsonObject> edit)
    {
        JsonObject root = Read();
        edit(root);
        string file = Path.Combine(Path.GetTempPath(), "hc-tuning-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(file, root.ToJsonString());
        try
        {
            return GameData.Load(DataDirectory, file);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
