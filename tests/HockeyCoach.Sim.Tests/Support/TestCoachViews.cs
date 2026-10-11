using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Shift;

namespace HockeyCoach.Sim.Tests.Support;

/// <summary>Builds coach views and public events for unit tests of coaches.</summary>
internal static class TestCoachViews
{
    public static CoachView Make(
        Team team,
        int forwardIndex,
        int pairIndex,
        bool canChangeForwards,
        bool canChangeDefence,
        IReadOnlyDictionary<string, double>? playUses = null,
        TeamSide side = TeamSide.Home,
        CoachCall call = CoachCall.PeriodStart,
        CoachTimeouts? timeouts = null,
        IReadOnlyList<SimEvent>? events = null,
        CoachClock? clock = null,
        int ownGoals = 0,
        int opponentGoals = 0)
    {
        UnitIceTime[] Units(int count, int onIce) =>
            Enumerable.Range(0, count).Select(i => new UnitIceTime(0.0, 0, i == onIce, 0.0, 0.0)).ToArray();

        return new CoachView(
            side,
            team,
            clock ?? new CoachClock(1, 0.0, 3, 1200.0),
            call,
            call == CoachCall.Stoppage ? StoppageReason.GoalieFreeze : null,
            ownGoals,
            opponentGoals,
            new LineChoice(forwardIndex, pairIndex),
            new LineChoice(-1, -1),
            canChangeForwards,
            canChangeDefence,
            new IceTimeView(Units(team.ForwardLines.Count, forwardIndex), Units(team.DefencePairs.Count, pairIndex)),
            timeouts ?? CoachTimeouts.None(1, 1),
            playUses ?? new Dictionary<string, double>(),
            events ?? Array.Empty<SimEvent>());
    }

    /// <summary>
    /// Placements with the given own skaters (home) on the ice, five away skaters 901–905 and goalies 900 / 906; the
    /// puck is loose at node 0.
    /// </summary>
    public static PlacementSnapshot Placement(IEnumerable<int> homeSkaters)
    {
        var players = new List<PlayerPlacement>();
        int node = 1;
        foreach (int id in homeSkaters)
        {
            players.Add(new PlayerPlacement(id, TeamSide.Home, node++, false));
        }

        for (int i = 1; i <= 5; i++)
        {
            players.Add(new PlayerPlacement(900 + i, TeamSide.Away, node++, false));
        }

        players.Add(new PlayerPlacement(900, TeamSide.Home, 0, true));
        players.Add(new PlayerPlacement(906, TeamSide.Away, 54, true));
        return new PlacementSnapshot(players, null, 0);
    }

    public static EventContext Context(double time, IEnumerable<int> homeSkaters, int period = 1, string? playId = null, string? systemId = null) =>
        new(period, time, new Strength(5, 5), Placement(homeSkaters), playId!, systemId!);

    /// <summary>The skater ids of a trio and a pair.</summary>
    public static int[] Unit(Team team, int trio, int pair) =>
        team.ForwardLines[trio].Skaters.Concat(team.DefencePairs[pair].Skaters).Select(s => s.Id).ToArray();
}
