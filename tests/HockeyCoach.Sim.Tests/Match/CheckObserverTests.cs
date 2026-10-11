using HockeyCoach.AI;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Match;

/// <summary>The read-only check observer used by the batch report.</summary>
public class CheckObserverTests
{
    private sealed class Recorder : ICheckObserver
    {
        public List<CheckObservation> Checks { get; } = new();

        public void OnCheck(CheckObservation observation) => Checks.Add(observation);
    }

    [Fact]
    public void Observer_DoesNotChangeTheMatch_AndSeesEveryFaceoffShotAndPass()
    {
        var data = ShiftTestData.Data;
        var recorder = new Recorder();
        MatchResult observed = MatchSimulator.Run(
            MatchTestData.Setup,
            new RotationCoach(data.Plays, HarnessTeams.System(data, "forecheck212")),
            new RotationCoach(data.Plays, HarnessTeams.System(data, "trap122")),
            new MatchStreams(4UL).Simulation(),
            recorder);
        MatchResult plain = MatchCommand.Play(data, MatchTestData.Setup, "forecheck212", "trap122", 4UL);

        Assert.Equal(MatchJson.Write(plain, MatchTestData.Setup, 4UL), MatchJson.Write(observed, MatchTestData.Setup, 4UL));
        List<SimEvent> events = observed.Log.Events.ToList();
        Assert.Equal(events.OfType<FaceoffEvent>().Count(), recorder.Checks.Count(c => c.CheckId == "faceoff"));
        Assert.Equal(events.OfType<ShotEvent>().Count(s => s.Outcome is ShotOutcome.Goal or ShotOutcome.Saved), recorder.Checks.Count(c => c.CheckId == "shot"));
        Assert.Equal(events.OfType<ShotEvent>().Count(s => s.Outcome == ShotOutcome.Goal), recorder.Checks.Count(c => c.CheckId == "shot" && c.Success));
        // A pass out of the defensive zone is resolved by the breakout check (D-039), so pass events = pass + breakout passes.
        int passes = events.OfType<PassEvent>().Count();
        Assert.InRange(recorder.Checks.Count(c => c.CheckId == "pass"), 1, passes);
        Assert.True(recorder.Checks.Count(c => c.CheckId is "pass" or "breakout") >= passes);
        Assert.Contains(recorder.Checks, c => c.CheckId == "breakout");
        Assert.All(recorder.Checks, c => Assert.InRange(c.Probability, 0.0, 1.0));
        Assert.All(recorder.Checks.Where(c => !double.IsNaN(c.AttackerEnergy)), c => Assert.InRange(c.AttackerEnergy, 0.0, 1.0));
        Assert.True(recorder.Checks.Select(c => (c.Period, c.Time)).SequenceEqual(recorder.Checks.Select(c => (c.Period, c.Time)).OrderBy(x => x.Period).ThenBy(x => x.Time)));
    }
}
