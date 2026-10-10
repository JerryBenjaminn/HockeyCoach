using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.State;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.State;

/// <summary>Milestone 3 state formulas (data-schema.md O-5, O-6, O-9, O-11) and containers.</summary>
public class StateModelTests
{
    [Fact]
    public void ReductionFactor_IsOneAtTheReference_AndFallsWithTheStat()
    {
        Assert.Equal(1.0, StateDynamics.ReductionFactor(0.03, 10.5, 10.5), 12);
        Assert.Equal(0.715, StateDynamics.ReductionFactor(0.03, 20, 10.5), 12);
        Assert.True(StateDynamics.ReductionFactor(0.03, 5, 10.5) > 1.0);
    }

    [Fact]
    public void BenchRecovery_IsExponential_TowardOne()
    {
        double e = StateDynamics.BenchRecovered(0.7, 90, 0.011);

        Assert.Equal(1.0 - (0.3 * Math.Exp(-0.99)), e, 12);
        Assert.Equal(0.7, StateDynamics.BenchRecovered(0.7, 0, 0.011), 12);
        Assert.True(StateDynamics.BenchRecovered(0.7, 10000, 0.011) <= 1.0);
    }

    [Fact]
    public void EnergyModifier_HurtsATiredAttacker_AndHelpsAgainstATiredDefender()
    {
        Assert.Equal(0.0, StateDynamics.EnergyModifier(-1.2, 1.0, 1.0), 12);
        Assert.Equal(-0.6, StateDynamics.EnergyModifier(-1.2, 0.5, 1.0), 12);
        Assert.Equal(0.6, StateDynamics.EnergyModifier(-1.2, 1.0, 0.5), 12);
        Assert.Equal(0.6, StateDynamics.EnergyModifier(-1.2, double.NaN, 0.5), 12);
    }

    [Fact]
    public void OrganizationAfterTurnover_FollowsO6()
    {
        // Offensive-zone loss: 1 - 0.65 = 0.35; after a shot the drop is halved: 1 - 0.325 = 0.675; one committed player: -0.1.
        Assert.Equal(0.35, StateDynamics.OrganizationAfterTurnover(1.0, 0.65, 1.0, 0.1, 0), 12);
        Assert.Equal(0.675, StateDynamics.OrganizationAfterTurnover(1.0, 0.65, 0.5, 0.1, 0), 12);
        Assert.Equal(0.575, StateDynamics.OrganizationAfterTurnover(1.0, 0.65, 0.5, 0.1, 1), 12);
        Assert.Equal(0.2, StateDynamics.OrganizationAfterTurnover(0.2, 0.35, 1.0, 0.1, 0), 12);
        Assert.Equal(0.0, StateDynamics.OrganizationAfterTurnover(0.1, 0.65, 1.0, 0.1, 3), 12);
    }

    [Fact]
    public void OrganizationRecovery_ScalesWithTheMeanStat()
    {
        Assert.Equal(0.04, StateDynamics.OrganizationRecoveryPerSecond(0.04, 0.06, 10.5, 10.5), 12);
        Assert.Equal(0.04 * 1.3, StateDynamics.OrganizationRecoveryPerSecond(0.04, 0.06, 15.5, 10.5), 12);
    }

    [Fact]
    public void OrganizationAndPressureModifiers_AreLinear()
    {
        Assert.Equal(0.0, StateDynamics.OrganizationModifier(0.6, 1.0), 12);
        Assert.Equal(0.6, StateDynamics.OrganizationModifier(0.6, 0.0), 12);
        Assert.Equal(0.3 * 0.64, StateDynamics.PressureModifier(0.3, 0.64, 1.0), 12);
    }

    [Theory]
    [InlineData(1, 0.0)]
    [InlineData(2, 0.0)]
    [InlineData(3, 0.08)]
    [InlineData(8, 0.48)]
    [InlineData(9, 0.5)]
    [InlineData(20, 0.5)]
    public void FamiliarityPenalty_MatchesTheO11Examples(double uses, double penalty)
    {
        Assert.Equal(penalty, StateDynamics.FamiliarityPenalty(uses, 2, 0.08, 0.5), 12);
    }

    [Fact]
    public void FamiliarityState_CountsStarts_AndIsHalvedAtIntermission()
    {
        var state = new FamiliarityState();
        for (int i = 0; i < 6; i++)
        {
            state.Start(TeamSide.Home, "a");
        }

        state.Multiply(0.5);

        Assert.Equal(3.0, state.Uses(TeamSide.Home, "a"));
        Assert.Equal(4.0, state.Start(TeamSide.Home, "a"));
        Assert.Equal(0.16, StateDynamics.FamiliarityPenalty(4.0, 2, 0.08, 0.5), 12);
        Assert.Equal(0.0, state.Uses(TeamSide.Away, "a"));
    }

    [Fact]
    public void GameState_StartsOrganizedWithoutPressure_AndClamps()
    {
        Team home = TestPlayers.Team("Home", 1);
        Team away = TestPlayers.Team("Away", 101);
        var state = new GameState(new LineupState(home, home.Goalies[0]), new LineupState(away, away.Goalies[0]), 1.0);

        Assert.Equal(1.0, state.Organization(TeamSide.Home));
        Assert.Equal(0.0, state.Pressure(TeamSide.Away));
        state.SetPressure(TeamSide.Away, 1.4);
        state.SetOrganization(TeamSide.Home, -0.2);
        Assert.Equal(1.0, state.Pressure(TeamSide.Away));
        Assert.Equal(0.0, state.Organization(TeamSide.Home));
        Assert.Equal(1.0, state.Energy.Get(home.Goalies[0].Id));
        Assert.True(state.Energy.IsBenched(home.Skaters[0].Id, out _));
    }

    [Fact]
    public void Lineup_TracksUnitsShiftsAndIceTime()
    {
        Team team = TestPlayers.Team("Home", 1);
        var lineup = new LineupState(team, team.Goalies[0]);
        Assert.Throws<InvalidOperationException>(() => lineup.OnIce);

        lineup.SetForwards(0, 0.0);
        lineup.SetPair(0, 0.0);
        lineup.AddIceTime(30);
        lineup.RestartShifts(0.0);

        Assert.Equal(30.0, lineup.ForwardSeconds(0));
        Assert.Equal(2, lineup.ForwardShifts(0));
        Assert.Equal(team.ForwardLines[0], lineup.OnIce.Forwards);
    }
}
