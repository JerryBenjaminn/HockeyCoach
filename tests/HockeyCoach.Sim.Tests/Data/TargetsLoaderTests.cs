using HockeyCoach.Harness.Data;

namespace HockeyCoach.Sim.Tests.Data;

public class TargetsLoaderTests
{
    private const string Valid = """
        {
          "schemaVersion": 1,
          "_notes": "fixture",
          "metrics": {
            "goalsPerMatch": {"unit": "count", "scope": "perMatch", "min": 4, "max": 7, "status": "approved", "source": "vision"},
            "xg": {"unit": "xg", "scope": "perTeamPerMatch", "min": null, "max": null, "status": "placeholder", "source": "stats"}
          }
        }
        """;

    [Fact]
    public void ValidTargets_LoadInNameOrder()
    {
        LoadResult<TargetsConfig> result = TargetsLoader.Parse(Valid);

        Assert.Empty(result.Errors);
        Assert.Equal(new[] { "goalsPerMatch", "xg" }, result.Value!.Metrics.Select(m => m.Name));
        Assert.Equal(4, result.Value.Metrics[0].Min);
        Assert.Null(result.Value.Metrics[1].Max);
    }

    [Fact]
    public void ApprovedMetricWithoutRange_IsReported()
    {
        LoadResult<TargetsConfig> result = TargetsLoader.Parse(Valid.Replace("\"min\": 4", "\"min\": null", StringComparison.Ordinal));

        Assert.Contains("metrics.goalsPerMatch: approved metric needs both min and max", result.Errors);
    }

    [Fact]
    public void UnknownUnit_IsReported()
    {
        LoadResult<TargetsConfig> result = TargetsLoader.Parse(Valid.Replace("\"unit\": \"xg\"", "\"unit\": \"goals\"", StringComparison.Ordinal));

        Assert.Contains(result.Errors, e => e.StartsWith("metrics.xg.unit: unknown value goals", StringComparison.Ordinal));
    }

    [Fact]
    public void MinAboveMax_IsReported()
    {
        LoadResult<TargetsConfig> result = TargetsLoader.Parse(Valid.Replace("\"max\": 7", "\"max\": 3", StringComparison.Ordinal));

        Assert.Contains("metrics.goalsPerMatch: min (4) is greater than max (3)", result.Errors);
    }
}
