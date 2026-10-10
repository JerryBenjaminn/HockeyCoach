using System.Globalization;
using System.Reflection;
using HockeyCoach.AI;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Match;

/// <summary>D-077 (3) and (5): coaches see public information only, as snapshots, and decide simultaneously.</summary>
public class CoachViewTests
{
    /// <summary>Rotation coach that records every view it gets and can deviate at one stoppage.</summary>
    private sealed class RecordingCoach : ICoach
    {
        private readonly RotationCoach _inner;
        private readonly Func<CoachView, bool> _deviateAt;

        public RecordingCoach(string system, Func<CoachView, bool>? deviateAt = null)
        {
            _inner = new RotationCoach(ShiftTestData.Data.Plays, HarnessTeams.System(ShiftTestData.Data, system));
            _deviateAt = deviateAt ?? (_ => false);
        }

        public List<CoachView> Stoppages { get; } = new();

        public List<string> StoppageTexts { get; } = new();

        public List<CoachView> OnTheFlyViews { get; } = new();

        public bool Deviated { get; private set; }

        public CoachDecision AtStoppage(CoachView view)
        {
            Stoppages.Add(view);
            StoppageTexts.Add(Describe(view));
            CoachDecision decision = _inner.AtStoppage(view);
            if (!Deviated && _deviateAt(view))
            {
                Deviated = true;

                // Keep the units on the ice (always legal) and regroup instead of rush.
                var plan = new ShiftPlan(decision.Plan.Plays, decision.Plan.System, TransitionInstruction.Regroup, new SystemModeInstructions(true, 2, true));
                return new CoachDecision(new LineChoice(view.ForwardIndex, view.PairIndex), plan);
            }

            return decision;
        }

        public LineChoice OnTheFly(CoachView view)
        {
            OnTheFlyViews.Add(view);
            return _inner.OnTheFly(view);
        }
    }

    private static string Describe(CoachView v)
    {
        string F(double d) => d.ToString("R", CultureInfo.InvariantCulture);
        return string.Join(
            "|",
            v.Side,
            v.Team.Name,
            v.Clock.Period,
            F(v.Clock.Time),
            v.Clock.Periods,
            F(v.Clock.PeriodSeconds),
            v.OwnGoals,
            v.OpponentGoals,
            v.OnIce.ForwardIndex,
            v.OnIce.PairIndex,
            v.OpponentOnIce.ForwardIndex,
            v.OpponentOnIce.PairIndex,
            v.CanChangeForwards,
            v.CanChangeDefence,
            v.Call,
            v.StoppageReason?.ToString() ?? "-",
            string.Join(",", v.IceTime.Trios.Concat(v.IceTime.Pairs).Select(u => F(u.TotalSeconds) + "/" + u.Shifts + "/" + u.OnIce + "/" + F(u.CurrentShiftSeconds) + "/" + F(u.BenchSeconds))),
            string.Join(",", v.PlayUses.Select(u => u.Key + "=" + F(u.Value))),
            v.Events.Count,
            v.Events.Count == 0 ? "-" : F(v.Events[v.Events.Count - 1].Context.Time));
    }

    private static (RecordingCoach Home, RecordingCoach Away) Play(ulong seed, Func<CoachView, bool>? homeDeviatesAt = null)
    {
        var home = new RecordingCoach("forecheck212", homeDeviatesAt);
        var away = new RecordingCoach("trap122");
        MatchSimulator.Run(MatchTestData.Setup, home, away, new MatchStreams(seed).Simulation());
        return (home, away);
    }

    [Fact]
    public void CoachView_ExposesExactlyThePublicFields()
    {
        string[] expected =
        {
            "Call", "CanChangeDefence", "CanChangeForwards", "Clock", "Events", "ForwardIndex", "IceTime", "OnIce", "OpponentGoals",
            "OpponentOnIce", "OwnGoals", "PairIndex", "Period", "PlayUses", "Side", "StoppageReason", "Team", "Time",
        };

        string[] actual = typeof(CoachView).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(typeof(CoachView))]
    [InlineData(typeof(CoachClock))]
    [InlineData(typeof(IceTimeView))]
    [InlineData(typeof(UnitIceTime))]
    public void CoachViewParts_ExposeNoHiddenState(Type type)
    {
        string[] hiddenNames = { "Energy", "Organization", "Pressure", "Random", "Plan", "Committed", "Familiarity" };
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            Assert.NotEqual("HockeyCoach.Sim.State", property.PropertyType.Namespace);
            Assert.False(typeof(IRandom).IsAssignableFrom(property.PropertyType), property.Name);
            Assert.NotEqual(typeof(ShiftPlan), property.PropertyType);
            Assert.DoesNotContain(hiddenNames, h => property.Name.Contains(h, StringComparison.Ordinal));
        }

        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void Views_AreSnapshots_ThatDoNotChangeAfterTheCoachIsAsked()
    {
        (RecordingCoach home, RecordingCoach away) = Play(5UL);

        foreach (RecordingCoach coach in new[] { home, away })
        {
            Assert.True(coach.Stoppages.Count > 10);
            Assert.Equal(coach.StoppageTexts, coach.Stoppages.Select(Describe));
        }
    }

    [Fact]
    public void Views_ShowTheClockScoreUnitsAndOwnIceTime()
    {
        (RecordingCoach home, RecordingCoach away) = Play(5UL);
        double periodSeconds = ShiftTestData.Data.Tuning.Time.PeriodSeconds;

        CoachView first = away.Stoppages[0];
        Assert.Equal(1, first.Period);
        Assert.Equal(0.0, first.Time);
        Assert.Equal(-1, first.OpponentOnIce.ForwardIndex);
        Assert.Empty(first.Events);

        CoachView second = away.Stoppages[1];
        LineChoice homeUnits = home.Stoppages[0].OnIce;
        Assert.Equal(0, second.OpponentOnIce.ForwardIndex);
        Assert.Equal(0, second.OpponentOnIce.PairIndex);
        Assert.NotEqual(homeUnits.ForwardIndex, second.OpponentOnIce.ForwardIndex);
        Assert.NotEmpty(second.Events);

        CoachView periodTwo = home.Stoppages.First(v => v.Period == 2);
        Assert.Equal(ShiftTestData.Data.Tuning.Time.Periods, periodTwo.Clock.Periods);
        Assert.Equal(CoachCall.PeriodStart, periodTwo.Call);
        Assert.Null(periodTwo.StoppageReason);
        Assert.Equal(periodSeconds, periodTwo.IceTime.Trios.Sum(u => u.TotalSeconds), 6);
        Assert.Equal(periodSeconds, periodTwo.IceTime.Pairs.Sum(u => u.TotalSeconds), 6);
        Assert.Equal(
            periodTwo.Events.OfType<Sim.Events.ShotEvent>().Count(e => e.Outcome == Sim.Events.ShotOutcome.Goal && e.Context.Placement.Find(e.ShooterId).Team == Sim.Events.TeamSide.Home),
            periodTwo.OwnGoals);
    }

    [Fact]
    public void Views_ShowStoppageReasonsAndCurrentShiftAndBenchTimes()
    {
        (RecordingCoach home, _) = Play(5UL);

        Assert.All(home.Stoppages.Where(v => v.Call == CoachCall.Stoppage), v => Assert.NotNull(v.StoppageReason));
        Assert.Contains(home.Stoppages, v => v.StoppageReason == Sim.Events.StoppageReason.Goal || v.StoppageReason == Sim.Events.StoppageReason.GoalieFreeze);
        Assert.All(home.OnTheFlyViews, v => Assert.Equal(CoachCall.OnTheFly, v.Call));

        foreach (CoachView v in home.Stoppages.Concat(home.OnTheFlyViews).Where(v => v.ForwardIndex >= 0))
        {
            UnitIceTime onIce = v.IceTime.Trios[v.ForwardIndex];
            Assert.True(onIce.OnIce);
            Assert.Equal(0.0, onIce.BenchSeconds);
            Assert.InRange(onIce.CurrentShiftSeconds, 0.0, v.Time);
            Assert.Single(v.IceTime.Trios, u => u.OnIce);
            Assert.Single(v.IceTime.Pairs, u => u.OnIce);
            Assert.All(v.IceTime.Trios.Where(u => !u.OnIce), u => Assert.InRange(u.BenchSeconds, 0.0, v.Time));
        }

        // Rotation: trio 0 left the ice exactly when trio 1 came on.
        CoachView third = home.Stoppages.First(v => v.Call == CoachCall.Stoppage && v.ForwardIndex == 1);
        UnitIceTime first = third.IceTime.Trios[0];
        Assert.False(first.OnIce);
        Assert.Equal(third.Time - third.IceTime.Trios[1].CurrentShiftSeconds, third.Time - first.BenchSeconds, 9);
    }

    [Fact]
    public void HomeDecision_CannotAffectTheAwayViewAtTheSameStoppage()
    {
        // Run B deviates from run A only at the home coach's first stoppage of period 2 (keeps units, regroups, all
        // system-mode instructions on). Everything before is identical, so the away views at that stoppage must match.
        (RecordingCoach homeA, RecordingCoach awayA) = Play(11UL);
        (RecordingCoach homeB, RecordingCoach awayB) = Play(11UL, v => v.Period == 2);

        Assert.True(homeB.Deviated);
        int index = homeB.Stoppages.FindIndex(v => v.Period == 2);
        Assert.Equal(homeA.StoppageTexts.Take(index + 1), homeB.StoppageTexts.Take(index + 1));
        Assert.Equal(awayA.StoppageTexts[index], awayB.StoppageTexts[index]);
        Assert.Equal(2, awayB.Stoppages[index].Period);
    }
}
