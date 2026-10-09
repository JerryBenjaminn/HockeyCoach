using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Data;

/// <summary>
/// Integration: the repository's real data/ files must load and validate. Asserts structure only, never balance
/// values, so designer tuning does not break the tests.
/// </summary>
public class RepositoryDataTests
{
    private static readonly string DataDirectory = Path.Combine(Fixtures.RepoRoot(), "data");

    [Fact]
    public void RinkJson_LoadsAndValidates()
    {
        LoadResult<Rink> result = RinkLoader.Load(Path.Combine(DataDirectory, GameData.RinkFile));

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Errors));
        Rink rink = result.Value!;
        Assert.Equal(rink.Length * rink.Width, rink.NodeCount);
        for (int id = 0; id < rink.NodeCount; id++)
        {
            Assert.Equal(id, rink.Flip(rink.Flip(id)));
            Assert.Equal(RinkZones.Flip(rink.ZoneOf(id)), rink.ZoneOf(rink.Flip(id)));
        }

        // data-schema.md: the goals are each other's images under the 180-degree rotation.
        int ownGoal = rink.IdOf(rink.OwnGoal.X, rink.OwnGoal.Y);
        Assert.Equal(rink.IdOf(rink.OpponentGoal.X, rink.OpponentGoal.Y), rink.Flip(ownGoal));
    }

    [Fact]
    public void TuningJson_LoadsAndValidatesAgainstRink()
    {
        Rink rink = RinkLoader.Load(Path.Combine(DataDirectory, GameData.RinkFile)).GetOrThrow(GameData.RinkFile);

        LoadResult<TuningConfig> result = TuningLoader.Load(Path.Combine(DataDirectory, GameData.TuningFile), rink.XgZones);

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Errors));
    }

    [Fact]
    public void AllDataFiles_LoadTogether()
    {
        GameData data = GameData.Load(DataDirectory);

        Assert.NotEmpty(data.Targets.Metrics);
        Assert.Equal(
            data.Rink.XgZones.OrderBy(z => z, StringComparer.Ordinal),
            data.Tuning.Shot.BaseXg.Keys);
    }

    [Fact]
    public void TuningJson_HasEveryCheckKindTheSchemaNames()
    {
        TuningConfig tuning = GameData.Load(DataDirectory).Tuning;

        Assert.Equal(CheckKind.OneSided, tuning.GetCheck("block").Kind);
        Assert.Equal(CheckSide.Defender, tuning.GetCheck("block").Side);
        Assert.Equal(CheckKind.OneSided, tuning.GetCheck("rebound").Kind);
        Assert.Equal(CheckKind.NoCheck, tuning.GetCheck("dumpIn").Kind);
        foreach (string id in new[] { "faceoff", "pass", "zoneEntryCarry", "deke", "breakout", "loosePuck", "hit" })
        {
            Assert.Equal(CheckKind.TwoSided, tuning.GetCheck(id).Kind);
        }
    }

    [Fact]
    public void EveryRollableCheck_ResolvesWithRealData()
    {
        GameData data = GameData.Load(DataDirectory);
        var skater = TestPlayers.Skater(1, 10);
        var goalie = TestPlayers.Goalie(2, 10);
        var participants = new CheckParticipants();
        foreach (string role in CheckRoles.All())
        {
            participants.With(role, CheckRoles.StatOwnerOf(role) == StatOwner.Goalie ? goalie : skater);
        }

        foreach (CheckDefinition check in data.Tuning.Checks.Values.Where(c => c.Kind != CheckKind.NoCheck))
        {
            double p = CheckResolver.Probability(data.Tuning.CheckFormula, check, participants, 0.0);
            Assert.InRange(p, data.Tuning.CheckFormula.MinProbability, data.Tuning.CheckFormula.MaxProbability);
        }

        foreach (string zone in data.Rink.XgZones)
        {
            double p = ShotResolver.GoalProbability(data.Tuning.CheckFormula, data.Tuning.Shot, zone, participants, 0.0);
            Assert.InRange(p, data.Tuning.Shot.MinProbability, data.Tuning.Shot.MaxProbability);
        }
    }
}
