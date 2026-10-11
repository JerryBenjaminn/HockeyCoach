using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Shift;

namespace HockeyCoach.Sim.Tests.Shift;

/// <summary>The zone entry strength "N vs M" of data-schema.md M-8 as corrected by D-080 (Q-065) and D-084 (E-006).</summary>
public class EntryStrengthTests
{
    private static readonly GridPoint Puck = new(4, 2);

    /// <summary>Skaters placed by x only; y does not matter for M-8.</summary>
    private static Dictionary<Position, GridPoint> Xs(int c, int lw, int rw, int ld, int rd) => new()
    {
        [Position.Center] = new GridPoint(c, 2),
        [Position.LeftWing] = new GridPoint(lw, 1),
        [Position.RightWing] = new GridPoint(rw, 3),
        [Position.LeftDefence] = new GridPoint(ld, 1),
        [Position.RightDefence] = new GridPoint(rd, 3),
    };

    private static EntryNumbers Count(Position carrier, Dictionary<Position, GridPoint> attackers, Dictionary<Position, GridPoint> defenders) =>
        EntryStrength.Count(Puck, carrier, attackers, defenders);

    [Fact]
    public void Carrier_IsAlwaysCounted_EvenWhenHisNodeIsBehindThePuck()
    {
        // All attackers behind the puck's x; the carrier's own node is behind too (e.g. a passer read elsewhere).
        EntryNumbers numbers = Count(Position.LeftDefence, Xs(1, 1, 1, 1, 1), Xs(0, 0, 0, 0, 0));

        Assert.Equal(1, numbers.Attackers);
    }

    [Fact]
    public void Carrier_AtThePuck_IsCountedOnce()
    {
        EntryNumbers numbers = Count(Position.Center, Xs(4, 0, 0, 0, 0), Xs(0, 0, 0, 0, 0));

        Assert.Equal(1, numbers.Attackers);
    }

    [Fact]
    public void LevelAttacker_IsNotCounted()
    {
        EntryNumbers numbers = Count(Position.Center, Xs(4, 4, 4, 2, 2), Xs(0, 0, 0, 0, 0));

        Assert.Equal(1, numbers.Attackers);
    }

    [Fact]
    public void AttackerAheadOfThePuck_IsCounted_AttackerBehindIsNot()
    {
        EntryNumbers numbers = Count(Position.Center, Xs(4, 6, 9, 3, 0), Xs(0, 0, 0, 0, 0));

        Assert.Equal(3, numbers.Attackers);
    }

    [Fact]
    public void LevelDefender_IsNotCounted()
    {
        EntryNumbers numbers = Count(Position.Center, Xs(4, 0, 0, 0, 0), Xs(4, 4, 4, 4, 4));

        Assert.Equal(0, numbers.Defenders);
    }

    [Fact]
    public void DefenderBehindThePuck_IsNotCounted()
    {
        EntryNumbers numbers = Count(Position.Center, Xs(4, 0, 0, 0, 0), Xs(3, 2, 0, 3, 1));

        Assert.Equal(0, numbers.Defenders);
    }

    [Fact]
    public void DefenderBetweenThePuckAndHisGoal_IsCounted()
    {
        // Two between the puck and the goal, one level, two behind.
        EntryNumbers numbers = Count(Position.Center, Xs(4, 0, 0, 0, 0), Xs(5, 3, 4, 9, 2));

        Assert.Equal(2, numbers.Defenders);
    }

    [Fact]
    public void FullExample_TwoVsTwo()
    {
        // Carrier C at the puck, LW level (not counted), RW ahead, D pair behind; defenders: two deep, one level, two behind.
        EntryNumbers numbers = Count(Position.Center, Xs(4, 4, 6, 2, 2), Xs(4, 3, 3, 8, 7));

        Assert.Equal(new EntryNumbers(2, 2), numbers);
    }
}
