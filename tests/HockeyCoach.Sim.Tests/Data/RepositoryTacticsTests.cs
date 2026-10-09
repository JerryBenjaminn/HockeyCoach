using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Data;

/// <summary>
/// Integration: the repository's data/plays and data/systems load and validate, and so do the mirrored plays.
/// Structure only, no balance assertions.
/// </summary>
public class RepositoryTacticsTests
{
    private static readonly GameData Data = GameData.Load(Path.Combine(Fixtures.RepoRoot(), "data"));

    [Fact]
    public void PlaysAndSystems_LoadInFileNameOrder_WithIdsEqualToFileNames()
    {
        string playsDir = Path.Combine(Fixtures.RepoRoot(), "data", GameData.PlaysDirectory);
        string systemsDir = Path.Combine(Fixtures.RepoRoot(), "data", GameData.SystemsDirectory);
        string[] playFiles = Directory.GetFiles(playsDir, "*.json").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n, StringComparer.Ordinal).ToArray()!;
        string[] systemFiles = Directory.GetFiles(systemsDir, "*.json").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n, StringComparer.Ordinal).ToArray()!;

        Assert.Equal(playFiles, Data.Plays.Select(p => p.Id));
        Assert.Equal(systemFiles, Data.Systems.Select(s => s.Id));
    }

    [Fact]
    public void EveryMirroredPlay_IsValidToo()
    {
        foreach (Play play in Data.Plays)
        {
            IReadOnlyList<string> errors = PlayValidator.Validate(PlayMirror.Mirror(play, Data.Rink), Data.Rink, Data.Tuning.Plays);
            Assert.True(errors.Count == 0, play.Id + " mirrored: " + string.Join("; ", errors));
        }
    }

    [Fact]
    public void FirstSystems_Exist()
    {
        // D-021: the first two systems.
        Assert.Contains(Data.Systems, s => s.Id == "forecheck212");
        Assert.Contains(Data.Systems, s => s.Id == "trap122");
    }

    [Fact]
    public void RealRink_SatisfiesTheMirrorAndGoalRules()
    {
        Rink rink = Data.Rink;

        Assert.Equal(rink.CentreLaneY, rink.OpponentGoal.Y);
        Assert.Equal(rink.OpponentGoal, rink.Rotate(rink.OwnGoal));
        Assert.False(rink.GetNode(rink.OpponentGoal.X, rink.OpponentGoal.Y).IsSlot);
    }
}
