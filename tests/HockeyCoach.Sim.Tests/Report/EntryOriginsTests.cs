using System.Text;
using HockeyCoach.Harness.Batch;
using HockeyCoach.Harness.Report;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Tests.Match;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Report;

/// <summary>The back-check report section (data-schema.md, Paluukarvaus suorahyökkäyksessä; D-085): entry origins from the log.</summary>
public class EntryOriginsTests
{
    private static IEnumerable<IReadOnlyList<SimEvent>> Logs() => MatchTestData.Sample(4).Select(r => r.Log.Events);

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
