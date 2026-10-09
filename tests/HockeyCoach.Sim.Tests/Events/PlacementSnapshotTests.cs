using HockeyCoach.Sim.Events;

namespace HockeyCoach.Sim.Tests.Events;

public class PlacementSnapshotTests
{
    [Fact]
    public void Snapshot_SortsPlayersById_AndCountsSkatersPerTeam()
    {
        PlacementSnapshot snapshot = EventTestData.FiveOnFive();

        Assert.Equal(snapshot.Players.Select(p => p.PlayerId).OrderBy(id => id), snapshot.Players.Select(p => p.PlayerId));
        Assert.Equal(5, snapshot.SkaterCount(TeamSide.Home));
        Assert.Equal(5, snapshot.SkaterCount(TeamSide.Away));
        Assert.Equal(1, snapshot.PuckCarrierId);
        Assert.False(snapshot.IsPuckLoose);
    }

    [Fact]
    public void LoosePuck_HasNoCarrier()
    {
        PlacementSnapshot snapshot = EventTestData.FiveOnFive(carrierId: null, puckNode: 40);

        Assert.True(snapshot.IsPuckLoose);
        Assert.Equal(40, snapshot.PuckNodeId);
    }

    [Fact]
    public void Carrier_MustBeOnTheIce()
    {
        Assert.Throws<ArgumentException>(() => EventTestData.FiveOnFive(carrierId: 99));
    }

    [Fact]
    public void PuckNode_MustMatchCarrierNode()
    {
        Assert.Throws<ArgumentException>(() => EventTestData.FiveOnFive(carrierId: 1, puckNode: 28));
    }

    [Fact]
    public void DuplicatePlayer_IsRejected()
    {
        var players = new[]
        {
            new PlayerPlacement(1, TeamSide.Home, 10, false),
            new PlayerPlacement(1, TeamSide.Home, 11, false),
        };

        Assert.Throws<ArgumentException>(() => new PlacementSnapshot(players, null, 10));
    }
}
