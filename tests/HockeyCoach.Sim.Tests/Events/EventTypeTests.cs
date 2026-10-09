using HockeyCoach.Sim.Events;

namespace HockeyCoach.Sim.Tests.Events;

public class EventTypeTests
{
    /// <summary>Returns the visited type name, proving each event dispatches to its own visitor method.</summary>
    private sealed class NameVisitor : ISimEventVisitor<string>
    {
        public string Visit(FaceoffEvent e) => "Faceoff";

        public string Visit(ControlledZoneEntryEvent e) => "ControlledZoneEntry";

        public string Visit(DumpInEvent e) => "DumpIn";

        public string Visit(PassEvent e) => "Pass";

        public string Visit(ShotEvent e) => "Shot";

        public string Visit(TurnoverEvent e) => "Turnover";

        public string Visit(PuckBattleEvent e) => "PuckBattle";

        public string Visit(StoppageEvent e) => "Stoppage";

        public string Visit(LineChangeEvent e) => "LineChange";
    }

    public static IEnumerable<object[]> OneOfEachType()
    {
        EventContext c = EventTestData.Context();
        yield return new object[] { new FaceoffEvent(c, 2, 12, TeamSide.Away, FaceoffLocation.NeutralZone), "Faceoff" };
        yield return new object[] { new ControlledZoneEntryEvent(c, 1, new EntryNumbers(2, 2), ZoneEntryMethod.Carry, ZoneEntryOutcome.Kept), "ControlledZoneEntry" };
        yield return new object[] { new DumpInEvent(c, 1, BattleOutcome.NoWinner), "DumpIn" };
        yield return new object[] { new PassEvent(c, 1, 2, true, false), "Pass" };
        yield return new object[] { new ShotEvent(c, 1, 16, 0.05, ChanceClass.Moderate, ChanceType.OffensiveZone, null, ShotOutcome.Saved), "Shot" };
        yield return new object[] { new TurnoverEvent(c, 1, 11, 27), "Turnover" };
        yield return new object[] { new PuckBattleEvent(c, new[] { 1 }, new[] { 11, 12 }, BattleOutcome.Loss), "PuckBattle" };
        yield return new object[] { new StoppageEvent(c, StoppageReason.Goal), "Stoppage" };
        yield return new object[] { new LineChangeEvent(c, new[] { 1, 2, 3 }, new[] { 7, 8, 9 }), "LineChange" };
    }

    [Theory]
    [MemberData(nameof(OneOfEachType))]
    public void Accept_DispatchesToTheEventsOwnVisitMethod(SimEvent simEvent, string expected)
    {
        Assert.Equal(expected, simEvent.Accept(new NameVisitor()));
    }

    [Fact]
    public void EventSchema_HasExactlyNineEventTypes()
    {
        Type[] types = typeof(SimEvent).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(SimEvent))).ToArray();

        Assert.Equal(9, types.Length);
        Assert.Equal(9, typeof(ISimEventVisitor<int>).GetMethods().Length);
    }

    [Theory]
    [InlineData(ShotOutcome.Saved, null)]
    [InlineData(ShotOutcome.Blocked, null)]
    [InlineData(ShotOutcome.Goal, 16)]
    [InlineData(ShotOutcome.Missed, 16)]
    public void Shot_StoppedByMatchesOutcome(ShotOutcome outcome, int? stoppedBy)
    {
        Assert.Throws<ArgumentException>(() => new ShotEvent(EventTestData.Context(), 1, stoppedBy, 0.1, null, ChanceType.Rush, null, outcome));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void Shot_RejectsXgOutsideUnitRange(double xg)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShotEvent(EventTestData.Context(), 1, null, xg, null, ChanceType.Rush, null, ShotOutcome.Goal));
    }

    [Fact]
    public void PuckBattle_NeedsParticipantsOnBothSides()
    {
        Assert.Throws<ArgumentException>(() => new PuckBattleEvent(EventTestData.Context(), new[] { 1 }, Array.Empty<int>(), BattleOutcome.Win));
    }

    [Fact]
    public void LineChange_NeedsUnitsOfEqualSize()
    {
        Assert.Throws<ArgumentException>(() => new LineChangeEvent(EventTestData.Context(), new[] { 1, 2 }, new[] { 7 }));
    }
}
