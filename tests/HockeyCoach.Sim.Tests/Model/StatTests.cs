using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tests.Model;

public class StatTests
{
    [Fact]
    public void SkaterStats_ConstructorArguments_MapToMatchingStats()
    {
        var stats = new SkaterStats(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);

        Assert.Equal(1, stats[SkaterStat.Speed]);
        Assert.Equal(2, stats[SkaterStat.Agility]);
        Assert.Equal(3, stats[SkaterStat.Endurance]);
        Assert.Equal(4, stats[SkaterStat.Hands]);
        Assert.Equal(5, stats[SkaterStat.Passing]);
        Assert.Equal(6, stats[SkaterStat.ShotAccuracy]);
        Assert.Equal(7, stats[SkaterStat.ShotPower]);
        Assert.Equal(8, stats[SkaterStat.Positioning]);
        Assert.Equal(9, stats[SkaterStat.Awareness]);
        Assert.Equal(10, stats[SkaterStat.Strength]);
        Assert.Equal(11, stats[SkaterStat.Discipline]);
        Assert.Equal(12, stats[SkaterStat.Faceoffs]);
    }

    [Fact]
    public void GoalieStats_ConstructorArguments_MapToMatchingStats()
    {
        var stats = new GoalieStats(1, 2, 3, 4, 5, 6);

        Assert.Equal(1, stats[GoalieStat.Reflexes]);
        Assert.Equal(2, stats[GoalieStat.Positioning]);
        Assert.Equal(3, stats[GoalieStat.Mobility]);
        Assert.Equal(4, stats[GoalieStat.ReboundControl]);
        Assert.Equal(5, stats[GoalieStat.PuckHandling]);
        Assert.Equal(6, stats[GoalieStat.MentalToughness]);
    }

    [Fact]
    public void SkaterStats_With_ReturnsCopyAndLeavesOriginalUnchanged()
    {
        var original = SkaterStats.Uniform(10);
        var changed = original.With(SkaterStat.Passing, 18);

        Assert.Equal(10, original[SkaterStat.Passing]);
        Assert.Equal(18, changed[SkaterStat.Passing]);
        Assert.Equal(10, changed[SkaterStat.Hands]);
    }

    [Fact]
    public void SkaterStats_FromArray_Throws_WhenCountIsWrong()
    {
        Assert.Throws<ArgumentException>(() => SkaterStats.FromArray(new int[11]));
    }

    [Theory]
    [InlineData("speed", SkaterStat.Speed)]
    [InlineData("agility", SkaterStat.Agility)]
    [InlineData("endurance", SkaterStat.Endurance)]
    [InlineData("hands", SkaterStat.Hands)]
    [InlineData("passing", SkaterStat.Passing)]
    [InlineData("shotAccuracy", SkaterStat.ShotAccuracy)]
    [InlineData("shotPower", SkaterStat.ShotPower)]
    [InlineData("positioning", SkaterStat.Positioning)]
    [InlineData("awareness", SkaterStat.Awareness)]
    [InlineData("strength", SkaterStat.Strength)]
    [InlineData("discipline", SkaterStat.Discipline)]
    [InlineData("faceoffs", SkaterStat.Faceoffs)]
    public void StatNames_RoundTripsSkaterDataNames(string name, SkaterStat stat)
    {
        Assert.True(StatNames.TryParseSkater(name, out SkaterStat parsed));
        Assert.Equal(stat, parsed);
        Assert.Equal(name, StatNames.ToName(stat));
    }

    [Theory]
    [InlineData("reflexes", GoalieStat.Reflexes)]
    [InlineData("positioning", GoalieStat.Positioning)]
    [InlineData("mobility", GoalieStat.Mobility)]
    [InlineData("reboundControl", GoalieStat.ReboundControl)]
    [InlineData("puckHandling", GoalieStat.PuckHandling)]
    [InlineData("mentalToughness", GoalieStat.MentalToughness)]
    public void StatNames_RoundTripsGoalieDataNames(string name, GoalieStat stat)
    {
        Assert.True(StatNames.TryParseGoalie(name, out GoalieStat parsed));
        Assert.Equal(stat, parsed);
        Assert.Equal(name, StatNames.ToName(stat));
    }

    [Theory]
    [InlineData("Speed")]
    [InlineData("reflexes")]
    [InlineData("")]
    public void StatNames_RejectsUnknownOrMiscasedSkaterNames(string name)
    {
        Assert.False(StatNames.TryParseSkater(name, out _));
    }

    [Fact]
    public void StatNames_ResolvesPositioningByOwner()
    {
        Assert.True(StatNames.TryParse(StatOwner.Skater, "positioning", out StatRef skater));
        Assert.True(StatNames.TryParse(StatOwner.Goalie, "positioning", out StatRef goalie));

        Assert.Equal(StatRef.Of(SkaterStat.Positioning), skater);
        Assert.Equal(StatRef.Of(GoalieStat.Positioning), goalie);
        Assert.NotEqual(skater, goalie);
    }

    [Fact]
    public void Skater_GetStat_Throws_ForGoalieStat()
    {
        var skater = new Skater(1, "A", Position.Center, SkaterStats.Uniform(10));

        Assert.Throws<InvalidOperationException>(() => skater.GetStat(StatRef.Of(GoalieStat.Reflexes)));
    }

    [Fact]
    public void Goalie_GetStat_ReadsGoalieStat()
    {
        var goalie = new Goalie(1, "G", GoalieStats.Uniform(10).With(GoalieStat.Reflexes, 17));

        Assert.Equal(17, goalie.GetStat(StatRef.Of(GoalieStat.Reflexes)));
    }
}
