using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Shift;

/// <summary>Mechanics visible in the log, checked on the first seeds where the situation occurs.</summary>
public class ShiftScenarioTests
{
    private static ShiftSetup AtSpot(string spot)
    {
        ShiftSetup s = ShiftTestData.Setup();
        return new ShiftSetup(s.Rink, s.Tuning, s.Home, s.Away, spot, 1, 0.0, s.MaxSteps);
    }

    [Fact]
    public void OffensiveFaceoffPlay_GivesThePlaysCarrierThePuck_WithoutAPass()
    {
        ShiftSetup setup = AtSpot("offensiveLeft");
        bool seen = false;
        for (ulong seed = 1; seed <= 200 && !seen; seed++)
        {
            ShiftResult result = ShiftTestData.Run(seed, setup);
            var faceoff = (FaceoffEvent)result.Log.Events[0];
            Assert.Equal(FaceoffLocation.AwayZone, faceoff.Location);
            if (faceoff.Winner != TeamSide.Home)
            {
                continue;
            }

            // offensiveFaceoffPointShot (D-060): LD gets the draw, RW drives the net, LD shoots. No pass event.
            Assert.Equal(4, faceoff.Context.Placement.PuckCarrierId); // home LD (ids: LW 1, C 2, RW 3, LD 4, RD 5)
            var shot = Assert.IsType<ShotEvent>(result.Log.Events[1]);
            Assert.Equal(4, shot.ShooterId);
            Assert.Equal(ChanceType.Faceoff, shot.ChanceType);
            Assert.Equal("offensiveFaceoffPointShot", shot.Context.PlayId);
            Assert.Equal("trap122", shot.Context.SystemId);
            int netFront = ShiftTestData.Data.Rink.IdOf(8, 2);
            Assert.Equal(netFront, shot.Context.Placement.Find(3)!.NodeId); // RW screens at the net front
            seen = true;
        }

        Assert.True(seen);
    }

    [Theory]
    [InlineData("center", 4)]
    [InlineData("neutralDefensiveLeft", 4)]
    [InlineData("neutralDefensiveRight", 5)]
    public void FaceoffWithoutAPlay_GivesThePuckToTheDotSideDefenceman(string spot, int homeDefenceId)
    {
        // Home has no faceoff play for these spots (only offensiveLeft in data); away wins are skipped.
        ShiftSetup setup = AtSpot(spot);
        bool seen = false;
        for (ulong seed = 1; seed <= 50 && !seen; seed++)
        {
            var faceoff = (FaceoffEvent)ShiftTestData.Run(seed, setup).Log.Events[0];
            if (faceoff.Winner != TeamSide.Home)
            {
                continue;
            }

            Assert.Equal(homeDefenceId, faceoff.Context.Placement.PuckCarrierId);
            seen = true;
        }

        Assert.True(seen);
    }

    [Fact]
    public void LoosePuckWonInTheOffensiveSlot_IsShotAtOnce_WithCreaseXgAtTheNetFront()
    {
        Rink rink = ShiftTestData.Data.Rink;
        int seen = 0;
        int creaseSeen = 0;
        for (ulong seed = 1; seed <= 300; seed++)
        {
            IReadOnlyList<SimEvent> events = ShiftTestData.Run(seed).Log.Events;
            for (int i = 0; i < events.Count - 1; i++)
            {
                if (events[i] is not PuckBattleEvent battle || battle.Outcome == BattleOutcome.NoWinner)
                {
                    continue;
                }

                PlacementSnapshot p = battle.Context.Placement;
                PlayerPlacement winner = p.Find(p.PuckCarrierId!.Value)!;
                GridPoint view = TeamFrame.ToTeamView(new GridPoint(rink.XOf(winner.NodeId), rink.YOf(winner.NodeId)), winner.Team, rink);
                bool slot = view.Y == 2 && (view.X == 7 || view.X == 8);
                if (!slot)
                {
                    Assert.IsNotType<ShotEvent>(events[i + 1]);
                    continue;
                }

                var shot = Assert.IsType<ShotEvent>(events[i + 1]);
                Assert.Equal(winner.PlayerId, shot.ShooterId);
                Assert.Null(shot.Context.PlayId);
                seen++;
                if (view.X == 8)
                {
                    creaseSeen++;
                }
            }
        }

        Assert.True(seen > 0);
        Assert.True(creaseSeen > 0);
    }

    [Fact]
    public void Dump_IsLoggedWithTheBattleOutcome_AndLeavesAPuckHolderOrALoosePuck()
    {
        bool seen = false;
        for (ulong seed = 1; seed <= 300 && !seen; seed++)
        {
            ShiftResult result = ShiftTestData.Run(seed);
            DumpInEvent? dump = ShiftTestData.Of<DumpInEvent>(result).FirstOrDefault();
            if (dump == null)
            {
                continue;
            }

            Assert.Equal("dumpAndChase", dump.Context.PlayId);
            Assert.Equal(dump.BattleOutcome == BattleOutcome.NoWinner, dump.Context.Placement.IsPuckLoose);
            seen = true;
        }

        Assert.True(seen);
    }

    [Fact]
    public void FailedPass_IsAnInterceptionOrALoosePuckBattle()
    {
        int checkedPasses = 0;
        for (ulong seed = 1; seed <= 100; seed++)
        {
            IReadOnlyList<SimEvent> events = ShiftTestData.Run(seed).Log.Events;
            for (int i = 0; i < events.Count - 1; i++)
            {
                if (events[i] is not PassEvent { Succeeded: false } pass)
                {
                    continue;
                }

                SimEvent next = events[i + 1] is ControlledZoneEntryEvent ? events[i + 2] : events[i + 1];
                if (pass.Context.Placement.IsPuckLoose)
                {
                    Assert.IsType<PuckBattleEvent>(next);
                }
                else
                {
                    var turnover = Assert.IsType<TurnoverEvent>(next);
                    Assert.Equal(pass.PasserId, turnover.LoserId);
                    Assert.Equal(pass.Context.Placement.PuckCarrierId, turnover.TakerId);
                }

                checkedPasses++;
            }
        }

        Assert.True(checkedPasses > 0);
    }

    [Fact]
    public void Requirements_AcceptTheRepositoryTuning_AndListWhatAFixtureLacks()
    {
        Assert.Empty(ShiftTuningRequirements.Validate(ShiftTestData.Data.Tuning));

        TuningConfig small = TuningLoader.Parse(Fixtures.Read("tuning-small.json"), Fixtures.SmallRinkXgZones).Value!;
        IReadOnlyList<string> errors = ShiftTuningRequirements.Validate(small);

        Assert.Contains("checks.faceoff: missing (used by the shift simulation)", errors);
        Assert.Contains("time.secondsPerAction.systemStep: missing (used by the shift simulation)", errors);
    }
}
