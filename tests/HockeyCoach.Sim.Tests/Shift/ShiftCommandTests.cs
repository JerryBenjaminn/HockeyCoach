using System.Text.Json;
using HockeyCoach.Harness.Simulation;

namespace HockeyCoach.Sim.Tests.Shift;

public class ShiftCommandTests
{
    [Fact]
    public void Parse_DefaultsToSeed42WithTheBoard()
    {
        ShiftCommand.Options options = ShiftCommand.Parse(new[] { "shift" }, out string? error)!;

        Assert.Null(error);
        Assert.Equal(42UL, options.Seed);
        Assert.True(options.Board);
        Assert.False(options.Json);
    }

    [Fact]
    public void Parse_ReadsOptions_AndRejectsUnknownOnes()
    {
        ShiftCommand.Options options = ShiftCommand.Parse(new[] { "shift", "--seed", "7", "--no-board", "--json", "--away-system", "forecheck212" }, out _)!;

        Assert.Equal(7UL, options.Seed);
        Assert.False(options.Board);
        Assert.True(options.Json);
        Assert.Equal("forecheck212", options.AwaySystem);
        Assert.Null(ShiftCommand.Parse(new[] { "shift", "--colour" }, out string? error));
        Assert.Contains("--colour", error);
    }

    [Fact]
    public void Text_ShowsEveryEventWithABoard_AndTheResult()
    {
        string text = ShiftCommand.Run(ShiftTestData.Data, ShiftCommand.Parse(new[] { "shift" }, out _)!);

        Assert.Contains("Faceoff:", text);
        Assert.Contains("y=0 |", text);
        Assert.Contains("STOPPAGE:", text);
        Assert.Contains("End: ", text);
        Assert.Equal(text, ShiftCommand.Run(ShiftTestData.Data, ShiftCommand.Parse(new[] { "shift" }, out _)!));
    }

    [Fact]
    public void Json_IsValidAndHasEveryEvent()
    {
        string json = ShiftCommand.Run(ShiftTestData.Data, ShiftCommand.Parse(new[] { "shift", "--json" }, out _)!);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement events = document.RootElement.GetProperty("events");
        Assert.True(events.GetArrayLength() > 1);
        Assert.Equal("Faceoff", events[0].GetProperty("type").GetString());
        Assert.Equal("Stoppage", events[events.GetArrayLength() - 1].GetProperty("type").GetString());
        Assert.Equal(12, events[0].GetProperty("placement").GetProperty("players").GetArrayLength());
    }
}
