using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;

namespace HockeyCoach.Sim.Tests.Shift;

/// <summary>M-7 with the D-070 exception: a shot directly off a completed pass is free when the receiver was free.</summary>
public class ShotOffPassPressureTests
{
    private static readonly string[] Spots = { "center", "offensiveLeft", "offensiveRight", "neutralOffensiveLeft", "neutralDefensiveRight" };

    private sealed class Trace : IShotPressureTrace
    {
        public List<ShotPressureSample> Samples { get; } = new();

        public void OnShot(ShotPressureSample sample) => Samples.Add(sample);
    }

    private static ShiftSetup AtSpot(string spot)
    {
        ShiftSetup s = ShiftTestData.Setup();
        return new ShiftSetup(s.Rink, s.Tuning, s.Home, s.Away, spot, 1, 0.0, s.MaxSteps);
    }

    /// <summary>Every shot of many shifts with its pressure sample and the event before it in the log.</summary>
    private static List<(ShotEvent Shot, ShotPressureSample Sample, SimEvent? Previous)> Shots()
    {
        var shots = new List<(ShotEvent, ShotPressureSample, SimEvent?)>();
        foreach (string spot in Spots.Where(s => ShiftTestData.Data.Rink.FaceoffSpots.Any(f => f.Id == s)))
        {
            ShiftSetup setup = AtSpot(spot);
            for (ulong seed = 1; seed <= 150; seed++)
            {
                var trace = new Trace();
                ShiftResult result = ShiftSimulator.Run(setup, new Pcg32(seed), trace);
                List<SimEvent> events = result.Log.Events.ToList();
                List<int> shotIndices = events.Select((e, i) => (e, i)).Where(x => x.e is ShotEvent).Select(x => x.i).ToList();
                Assert.Equal(shotIndices.Count, trace.Samples.Count);
                for (int k = 0; k < shotIndices.Count; k++)
                {
                    int i = shotIndices[k];
                    SimEvent? previous = events.Take(i).LastOrDefault(e => e is not ControlledZoneEntryEvent and not LineChangeEvent);
                    shots.Add(((ShotEvent)events[i], trace.Samples[k], previous));
                }
            }
        }

        return shots;
    }

    private static readonly Lazy<List<(ShotEvent Shot, ShotPressureSample Sample, SimEvent? Previous)>> All = new(Shots);

    [Fact]
    public void ShotsOffAPass_AreSeen_BothWithAFreeAndACoveredReceiver()
    {
        List<ShotPressureSample> offPass = All.Value.Select(s => s.Sample).Where(s => s.OffPass).ToList();

        Assert.Contains(offPass, s => s.ReceiverFreeAtPass);
        Assert.Contains(offPass, s => !s.ReceiverFreeAtPass);
        Assert.Contains(All.Value, s => !s.Sample.OffPass);
    }

    [Fact]
    public void ReceiverFreeAtPass_EqualsSnapshotS_AtTheStartOfTheShootingBeat()
    {
        foreach ((ShotEvent _, ShotPressureSample sample, SimEvent? _) in All.Value.Where(s => s.Sample.OffPass))
        {
            Assert.True(sample.ReceiverFreeAtBeatStart.HasValue, "snapshot S was not traced for a shot off a pass");
            Assert.Equal(sample.ReceiverFreeAtPass, sample.ReceiverFreeAtBeatStart!.Value);
        }
    }

    [Fact]
    public void ShotEventField_IsPressureAtShot_UnlessOffAPassToAFreeReceiver()
    {
        foreach ((ShotEvent shot, ShotPressureSample sample, SimEvent? _) in All.Value)
        {
            bool expected = sample.PressureAtShot && !(sample.OffPass && sample.ReceiverFreeAtPass);
            Assert.Equal(expected, sample.UnderPressure);
            Assert.Equal(sample.UnderPressure, shot.UnderPressure);
            Assert.Equal(shot.ShooterId, sample.ShooterId);
        }
    }

    [Fact]
    public void Exception_OnlyRemovesPressure_NeverAddsIt()
    {
        Assert.All(All.Value, s => Assert.True(s.Sample.PressureAtShot || !s.Sample.UnderPressure));
        Assert.Contains(All.Value, s => s.Sample.PressureAtShot && !s.Sample.UnderPressure);
    }

    [Fact]
    public void ShotOffAPass_FollowsACompletedPassToTheShooter()
    {
        foreach ((ShotEvent shot, ShotPressureSample sample, SimEvent? previous) in All.Value.Where(s => s.Sample.OffPass))
        {
            PassEvent pass = Assert.IsType<PassEvent>(previous);
            Assert.True(pass.Succeeded);
            Assert.Equal(shot.ShooterId, pass.ReceiverId);
        }
    }

    [Fact]
    public void SecondChanceFaceoffWinAndFailedPassShots_AreNotOffAPass()
    {
        foreach ((ShotEvent _, ShotPressureSample sample, SimEvent? previous) in All.Value)
        {
            bool notAfterCompletedPass = previous is PuckBattleEvent or FaceoffEvent or ShotEvent or DumpInEvent or TurnoverEvent
                || (previous is PassEvent pass && !pass.Succeeded);
            if (notAfterCompletedPass)
            {
                Assert.False(sample.OffPass);
            }
        }

        Assert.Contains(All.Value, s => s.Previous is FaceoffEvent);
        Assert.Contains(All.Value, s => s.Previous is PuckBattleEvent);
    }

    [Fact]
    public void Trace_DoesNotChangeTheEventLog()
    {
        ShiftSetup setup = AtSpot("center");
        for (ulong seed = 1; seed <= 20; seed++)
        {
            string plain = string.Join("\n", ShiftSimulator.Run(setup, new Pcg32(seed)).Log.Events.Select(EventText));
            string traced = string.Join("\n", ShiftSimulator.Run(setup, new Pcg32(seed), new Trace()).Log.Events.Select(EventText));
            Assert.Equal(plain, traced);
        }
    }

    private static string EventText(SimEvent e) => e.GetType().Name + "@" + e.Context.Time.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
        + (e is ShotEvent s ? ":" + s.ShooterId + ":" + s.UnderPressure + ":" + s.Outcome : string.Empty);
}
