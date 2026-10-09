using HockeyCoach.Sim.Events;

namespace HockeyCoach.Sim.Tests.Events;

/// <summary>Small hand-built event contexts for event tests.</summary>
internal static class EventTestData
{
    /// <summary>Home skaters 1..5 and goalie 6, away skaters 11..15 and goalie 16; home #1 carries the puck at node 27.</summary>
    public static PlacementSnapshot FiveOnFive(int? carrierId = 1, int puckNode = 27)
    {
        var players = new List<PlayerPlacement>();
        for (int i = 0; i < 5; i++)
        {
            players.Add(new PlayerPlacement(1 + i, TeamSide.Home, i == 0 ? 27 : 20 + i, false));
            players.Add(new PlayerPlacement(11 + i, TeamSide.Away, 30 + i, false));
        }

        players.Add(new PlayerPlacement(6, TeamSide.Home, 7, true));
        players.Add(new PlayerPlacement(16, TeamSide.Away, 47, true));
        return new PlacementSnapshot(players, carrierId, puckNode);
    }

    public static EventContext Context(int period = 1, double time = 0.0, string? playId = null, string? systemId = null)
    {
        return new EventContext(period, time, new Strength(5, 5), FiveOnFive(), playId!, systemId!);
    }
}
