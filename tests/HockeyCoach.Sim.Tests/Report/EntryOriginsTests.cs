using System.Text;
using HockeyCoach.AI;
using HockeyCoach.Harness.Batch;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Harness.Report;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Match;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Report;

/// <summary>The back-check report section (data-schema.md, Paluukarvaus suorahyökkäyksessä; D-085): entry origins from the log.</summary>
public class EntryOriginsTests
{
    private static IEnumerable<IReadOnlyList<SimEvent>> Logs() => MatchTestData.Sample(4).Select(r => r.Log.Events);

    private sealed class Recorder : ICheckObserver
    {
        public List<CheckObservation> Checks { get; } = new();

        public void OnCheck(CheckObservation observation) => Checks.Add(observation);
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    public void DefendersAhead_AtTheM8Moment_EqualTheLoggedM(ulong seed)
    {
        var data = ShiftTestData.Data;
        var recorder = new Recorder();
        MatchResult result = MatchSimulator.Run(
            MatchTestData.Setup,
            new RotationCoach(data.Plays, HarnessTeams.System(data, "forecheck212"), TransitionInstruction.Rush),
            new RotationCoach(data.Plays, HarnessTeams.System(data, "trap122"), TransitionInstruction.Regroup),
            new MatchStreams(seed).Simulation(),
            recorder);
        IReadOnlyList<int> carryNodes = recorder.Checks.Where(c => c.CheckId == "zoneEntryCarry").Select(c => c.PuckNodeId).ToList();
        List<ControlledZoneEntryEvent> kept = result.Log.Events.OfType<ControlledZoneEntryEvent>().Where(e => e.Outcome == ZoneEntryOutcome.Kept).ToList();

        IReadOnlyList<EntryOrigin> origins = EntryOrigins.Classify(result.Log.Events, MatchTestData.TeamOf, data.Rink, carryNodes);

        Assert.Equal(kept.Count, origins.Count);
        Assert.Contains(kept, e => e.Method == ZoneEntryMethod.Carry);
        Assert.Contains(kept, e => e.Method == ZoneEntryMethod.Pass);
        int compared = 0;
        for (int i = 0; i < kept.Count; i++)
        {
            // Q-075: a mirrored play can make a non-holder skate the carry; the observed puck node is then the holder's.
            if (kept[i].Method == ZoneEntryMethod.Carry && kept[i].Context.Placement.PuckCarrierId != kept[i].CarrierId)
            {
                continue;
            }

            Assert.Equal(kept[i].Numbers.Defenders, origins[i].Ahead);
            compared++;
        }

        Assert.True(compared > kept.Count / 2);
    }

    [Fact]
    public void EveryKeptEntry_IsClassifiedOnce_WithFiveDefendersSplitAroundThePuck()
    {
        foreach (IReadOnlyList<SimEvent> events in Logs())
        {
            List<ControlledZoneEntryEvent> kept = events.OfType<ControlledZoneEntryEvent>().Where(e => e.Outcome == ZoneEntryOutcome.Kept).ToList();
            IReadOnlyList<EntryOrigin> origins = EntryOrigins.Classify(events, MatchTestData.TeamOf, ShiftTestData.Data.Rink);

            Assert.Equal(kept.Count, origins.Count);
            for (int i = 0; i < kept.Count; i++)
            {
                Assert.Equal(5, origins[i].Behind + origins[i].Level + origins[i].Ahead);
                EntryNumbers n = kept[i].Numbers;
                EntryStrengthGroup expected = n.Attackers < n.Defenders ? EntryStrengthGroup.Disadvantage
                    : n.Attackers == n.Defenders ? EntryStrengthGroup.Even : EntryStrengthGroup.Advantage;
                Assert.Equal(expected, origins[i].Group);
                Assert.Equal(string.IsNullOrEmpty(kept[i].Context.PlayId) ? "rush" : "play", origins[i].Mode);
                Assert.Contains(origins[i].Zone, EntryOrigins.Zones);
                Assert.Contains(origins[i].Gain, EntryOrigins.Gains);
            }
        }
    }

    [Fact]
    public void EntryAfterATurnover_TakesTheTurnoverNodeZone_InTheTakersView()
    {
        int checkedEntries = 0;
        foreach (IReadOnlyList<SimEvent> events in Logs())
        {
            IReadOnlyList<EntryOrigin> origins = EntryOrigins.Classify(events, MatchTestData.TeamOf, ShiftTestData.Data.Rink);
            int kept = 0;
            TurnoverEvent? lastTurnover = null;
            foreach (SimEvent e in events)
            {
                if (e is TurnoverEvent t)
                {
                    lastTurnover = t;
                }
                else if (e is FaceoffEvent || e is PuckBattleEvent || e is DumpInEvent || e is StoppageEvent)
                {
                    lastTurnover = null;
                }
                else if (e is ControlledZoneEntryEvent entry && entry.Outcome == ZoneEntryOutcome.Kept)
                {
                    if (lastTurnover != null && MatchTestData.TeamOf(lastTurnover.TakerId) == MatchTestData.TeamOf(entry.CarrierId))
                    {
                        var rink = ShiftTestData.Data.Rink;
                        int x = MatchTestData.TeamOf(entry.CarrierId) == TeamSide.Home ? rink.XOf(lastTurnover.NodeId) : rink.Length - 1 - rink.XOf(lastTurnover.NodeId);
                        string zone = rink.ZoneAtX(x) switch { Sim.Model.RinkZone.Defensive => "own", Sim.Model.RinkZone.Neutral => "neutral", _ => "offensive" };
                        Assert.Equal("turnover", origins[kept].Gain);
                        Assert.Equal(zone, origins[kept].Zone);
                        checkedEntries++;
                    }

                    kept++;
                }
            }
        }

        Assert.True(checkedEntries > 0);
    }

    [Fact]
    public void Section_ListsTheGroupsAndIsDeterministic()
    {
        string Write()
        {
            var origins = new EntryOrigins();
            foreach (IReadOnlyList<SimEvent> events in Logs())
            {
                origins.Add(events, MatchTestData.TeamOf, ShiftTestData.Data.Rink);
            }

            var md = new StringBuilder();
            origins.Write(md);
            return md.ToString();
        }

        string text = Write();
        Assert.Equal(text, Write());
        Assert.Contains("| all | all | all |", text);
        Assert.Contains("| own | turnover |", text);
    }
}
