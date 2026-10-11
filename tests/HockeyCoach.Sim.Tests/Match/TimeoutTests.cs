using HockeyCoach.AI;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.State;
using HockeyCoach.Sim.Tests.Shift;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Match;

/// <summary>Timeout (data-schema.md AI-4, AI-5; D-077 item 6; Q-059 default).</summary>
public class TimeoutTests
{
    /// <summary>Rotation coach that calls a timeout at the first stoppage where <paramref name="when"/> holds and it may.</summary>
    private sealed class TimeoutCoach : ICoach
    {
        private readonly RotationCoach _inner;
        private readonly Func<CoachView, bool> _when;
        private readonly bool _ignoreRules;

        public TimeoutCoach(string system, Func<CoachView, bool> when, bool ignoreRules = false)
        {
            _inner = new RotationCoach(ShiftTestData.Data.Plays, HarnessTeams.System(ShiftTestData.Data, system));
            _when = when;
            _ignoreRules = ignoreRules;
        }

        public List<CoachView> Views { get; } = new();

        public int Calls { get; private set; }

        public CoachDecision AtStoppage(CoachView view)
        {
            Views.Add(view);
            CoachDecision d = _inner.AtStoppage(view);
            bool call = (view.Timeouts.CanCall || _ignoreRules) && _when(view);
            Calls += call ? 1 : 0;
            return new CoachDecision(d.Units, d.Plan, call);
        }

        public LineChoice OnTheFly(CoachView view) => _inner.OnTheFly(view);
    }

    private static MatchResult Run(ICoach home, ICoach away, ulong seed = 3UL) =>
        MatchSimulator.Run(MatchTestData.Setup, home, away, new MatchStreams(seed).Simulation());

    private static bool FirstStoppage(CoachView v) => v.Call == CoachCall.Stoppage;

    [Fact]
    public void Timeout_IsLoggedAfterTheStoppage_AndBothCoachesAreAskedAgain()
    {
        var home = new TimeoutCoach("forecheck212", FirstStoppage);
        var away = new TimeoutCoach("trap122", _ => false);
        MatchResult result = Run(home, away);

        List<SimEvent> events = result.Log.Events.ToList();
        int index = events.FindIndex(e => e is TimeoutEvent);
        Assert.True(index > 0);
        var timeout = (TimeoutEvent)events[index];
        Assert.Equal(TeamSide.Home, timeout.Team);
        Assert.IsType<StoppageEvent>(events[index - 1]);
        Assert.Same(events[index - 1].Context, timeout.Context);
        Assert.Single(events.OfType<TimeoutEvent>());
        Assert.Equal(1, home.Calls);

        // The stoppage view, then the after-timeout view at the same moment for both coaches.
        int first = home.Views.FindIndex(FirstStoppage);
        CoachView before = home.Views[first];
        CoachView after = home.Views[first + 1];
        Assert.True(before.Timeouts.CanCall);
        Assert.True(after.Timeouts.AfterTimeoutRound);
        Assert.False(after.Timeouts.CanCall);
        Assert.Equal(0, after.Timeouts.OwnLeft);
        Assert.Equal(before.Time, after.Time);
        Assert.Equal(before.CanChangeForwards, after.CanChangeForwards);
        Assert.Equal(before.CanChangeDefence, after.CanChangeDefence);
        Assert.Equal(before.IceTime.Trios.Select(t => t.TotalSeconds), after.IceTime.Trios.Select(t => t.TotalSeconds));
        Assert.Contains(after.Events, e => e is TimeoutEvent);
        Assert.True(away.Views[first + 1].Timeouts.AfterTimeoutRound);
        Assert.Equal(0, away.Views[first + 1].Timeouts.OpponentLeft);
        Assert.Equal(1, away.Views[first + 1].Timeouts.OwnLeft);

        // Clock does not run: the next event after the timeout is at the stoppage time or later, never a jump back.
        Assert.True(events[index + 1].Context.CompareTime(timeout.Context) >= 0);
        Assert.All(home.Views.Skip(first + 2), v => Assert.False(v.Timeouts.CanCall));
    }

    [Fact]
    public void BothTeams_MayCallAtTheSameStoppage_HomeFirst()
    {
        MatchResult result = Run(new TimeoutCoach("forecheck212", FirstStoppage), new TimeoutCoach("trap122", FirstStoppage));

        TimeoutEvent[] timeouts = result.Log.Events.OfType<TimeoutEvent>().ToArray();
        Assert.Equal(new[] { TeamSide.Home, TeamSide.Away }, timeouts.Select(t => t.Team));
        Assert.Same(timeouts[0].Context, timeouts[1].Context);
    }

    [Fact]
    public void Timeout_IsNotAllowedAtAPeriodStart_OrWhenUsed()
    {
        Assert.Throws<InvalidOperationException>(() => Run(new TimeoutCoach("forecheck212", v => v.Call == CoachCall.PeriodStart, ignoreRules: true), new TimeoutCoach("trap122", _ => false)));
        Assert.Throws<InvalidOperationException>(() => Run(new TimeoutCoach("forecheck212", v => v.Timeouts.AfterTimeoutRound || v.Call == CoachCall.Stoppage, ignoreRules: true), new TimeoutCoach("trap122", _ => false)));
    }

    [Fact]
    public void ReasonAllows_GoalFreezeOutOfPlayIcingOffside_NotPeriodEndOrPenalty()
    {
        Assert.True(CoachTimeouts.ReasonAllows(StoppageReason.Goal));
        Assert.True(CoachTimeouts.ReasonAllows(StoppageReason.GoalieFreeze));
        Assert.True(CoachTimeouts.ReasonAllows(StoppageReason.OutOfPlay));
        Assert.True(CoachTimeouts.ReasonAllows(StoppageReason.Icing));
        Assert.True(CoachTimeouts.ReasonAllows(StoppageReason.Offside));
        Assert.False(CoachTimeouts.ReasonAllows(StoppageReason.PeriodEnd));
        Assert.False(CoachTimeouts.ReasonAllows(StoppageReason.Penalty));
    }

    [Fact]
    public void TimeoutRest_RealizesBenchRecoveryThenRestsEveryone()
    {
        Team team = TestPlayers.Team("T", 1, trios: 1, pairs: 1);
        var energy = new EnergyState();
        Skater onIce = team.Skaters[0];
        Skater benched = team.Skaters[1];
        energy.Add(onIce.Id, 0.6);
        energy.Add(benched.Id, 0.5);
        foreach (Skater s in team.Skaters.Skip(2))
        {
            energy.Add(s.Id, 1.0);
        }

        energy.Bench(benched.Id, 100.0);
        const double rate = 0.011;
        TimeoutRest.Apply(energy, team.Skaters, 130.0, rate, 30.0);

        double Rest(double e, double t) => 1.0 - ((1.0 - e) * Math.Exp(-rate * t));
        Assert.Equal(Rest(0.6, 30.0), energy.Get(onIce.Id), 12);
        Assert.Equal(Rest(Rest(0.5, 30.0), 30.0), energy.Get(benched.Id), 12);
        Assert.True(energy.IsBenched(benched.Id, out double since));
        Assert.Equal(130.0, since);
        Assert.False(energy.IsBenched(onIce.Id, out _));
    }

    [Fact]
    public void RandomCoach_DrawsTheTimeoutFirst_OnlyWhenOneMayBeCalled()
    {
        Team team = TestPlayers.Team("Home", 1, trios: 4, pairs: 3);
        var data = ShiftTestData.Data;
        CoachView may = TestCoachViews.Make(team, 0, 0, true, true, call: CoachCall.Stoppage, timeouts: new CoachTimeouts(1, 1, false, true));
        CoachView mayNot = TestCoachViews.Make(team, 0, 0, true, true, call: CoachCall.Stoppage);

        var always = new RandomCoach(data.Plays, data.Systems, new AiRandomConfig(0.5, 1.0), new Pcg32(9UL));
        Assert.True(always.AtStoppage(may).CallTimeout);
        Assert.False(always.AtStoppage(mayNot).CallTimeout);

        // With a possible timeout the first draw is the chance (two NextUInt), so the trio is the stream's next bounded draw.
        var twin = new Pcg32(9UL);
        twin.NextDouble();
        int trio = twin.NextInt(4);
        Assert.Equal(trio, new RandomCoach(data.Plays, data.Systems, new AiRandomConfig(0.5, 0.0), new Pcg32(9UL)).AtStoppage(may).Units.ForwardIndex);
    }
}
