using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Shift;

namespace HockeyCoach.Sim.Tests.Support;

/// <summary>Builds coach views for unit tests of coaches.</summary>
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
        CoachTimeouts? timeouts = null)
    {
        UnitIceTime[] Units(int count, int onIce) =>
            Enumerable.Range(0, count).Select(i => new UnitIceTime(0.0, 0, i == onIce, 0.0, 0.0)).ToArray();

        return new CoachView(
            side,
            team,
            new CoachClock(1, 0.0, 3, 1200.0),
            call,
            call == CoachCall.Stoppage ? StoppageReason.GoalieFreeze : null,
            0,
            0,
            new LineChoice(forwardIndex, pairIndex),
            new LineChoice(-1, -1),
            canChangeForwards,
            canChangeDefence,
            new IceTimeView(Units(team.ForwardLines.Count, forwardIndex), Units(team.DefencePairs.Count, pairIndex)),
            timeouts ?? CoachTimeouts.None(1, 1),
            playUses ?? new Dictionary<string, double>(),
            Array.Empty<SimEvent>());
    }
}
