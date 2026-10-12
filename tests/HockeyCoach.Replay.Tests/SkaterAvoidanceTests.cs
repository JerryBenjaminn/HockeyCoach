using HockeyCoach.Replay.Core;
using HockeyCoach.Replay.Tests.Support;
using Newtonsoft.Json.Linq;

namespace HockeyCoach.Replay.Tests;

/// <summary>The contact rule helpers (D-089 A2).</summary>
public sealed class SkaterAvoidanceTests
{
    private static readonly PresentationConfig Config = ReplayConfigFile.Read();

    private static SkaterSettings Sk => Config.Skaters;

    private static RinkMapping Map() => new SyntheticMatch().Add("Faceoff", 1, 0, 27, null, SyntheticMatch.OnIce(27, 16, 27, 38), new JObject()).Build().Mapping;

    [Fact]
    public void Axis_PointsTowardsTheHomeEnd_EverywhereOnTheRink_SoItNeverFlips()
    {
        RinkMapping map = Map();
        for (double x = 0; x <= map.LengthMetres; x += 0.5)
        {
            for (double y = 0; y <= map.WidthMetres; y += 0.5)
            {
                RinkPoint v = SkaterAvoidance.Axis(map, new RinkPoint(x, y));
                Assert.True(v.X < -0.4, "axis " + v + " at " + x + ", " + y);
                Assert.Equal(1, SkaterAvoidance.Length(v), 9);
            }
        }
    }

    [Fact]
    public void Axis_InFrontOfANet_PointsTowardsThatTeamsGoal()
    {
        RinkMapping map = Map();
        RinkPoint net = map.NetOf(ReplayTeam.Home);
        RinkPoint slot = new RinkPoint(net.X + 6, net.Y + 5);
        RinkPoint v = SkaterAvoidance.Axis(map, slot);
        RinkPoint toNet = SkaterAvoidance.Unit(net.X - slot.X, net.Y - slot.Y);
        Assert.True((v.X * toNet.X) + (v.Y * toNet.Y) > 0.8);
    }

    [Fact]
    public void Turn_IsLimited_AndReachesTheTargetWhenClose()
    {
        var from = new RinkPoint(1, 0);
        RinkPoint half = SkaterAvoidance.Turn(from, new RinkPoint(0, 1), Math.PI / 4);
        Assert.Equal(Math.Cos(Math.PI / 4), half.X, 12);
        Assert.Equal(Math.Sin(Math.PI / 4), half.Y, 12);
        Assert.Equal(new RinkPoint(0, 1), SkaterAvoidance.Turn(from, new RinkPoint(0, 1), Math.PI));
        RinkPoint back = SkaterAvoidance.Turn(from, new RinkPoint(-1, 0), 0.1);
        Assert.Equal(Math.Sin(0.1), back.Y, 12);
    }

    [Fact]
    public void TeammatePush_AtFullWeight_SeparatesAlongTheAxisToTheMinimum()
    {
        var axis = new RinkPoint(0, 1);
        RinkPoint push = SkaterAvoidance.TeammatePush(new RinkPoint(0.5, 0.5), axis, 1, Sk);
        Assert.Equal(0, push.X);
        Assert.Equal((Sk.SeparationMinMetres - 0.5) / 2, push.Y, 12);
        Assert.Equal(new RinkPoint(0, 0), SkaterAvoidance.TeammatePush(new RinkPoint(0, Sk.SeparationMinMetres + 1), axis, 1, Sk));
        Assert.Equal(new RinkPoint(0, 0), SkaterAvoidance.TeammatePush(new RinkPoint(0, 0.5), axis, 0, Sk));
    }

    [Fact]
    public void TeammateAxis_IsAcrossTheRelativeMotion_OnPlayerIsSide_OrAlongTheOffsetWhenSlow()
    {
        RinkPoint fast = SkaterAvoidance.TeammateAxis(new RinkPoint(3, -0.5), new RinkPoint(-8, 0), 0, Sk);
        Assert.Equal(new RinkPoint(0, -1), fast);
        RinkPoint slow = SkaterAvoidance.TeammateAxis(new RinkPoint(0, 1.5), new RinkPoint(0, 0), 0, Sk);
        Assert.Equal(new RinkPoint(0, 1), slow);
        RinkPoint same = SkaterAvoidance.TeammateAxis(new RinkPoint(0, 0), new RinkPoint(0, 0), Math.PI / 2, Sk);
        Assert.Equal(1, same.Y, 12);
    }

    [Fact]
    public void Weights_AreFullWhenClose_AndZeroBeyondTheirRanges()
    {
        Assert.Equal(1, SkaterAvoidance.OpponentWeight(Sk.OpponentPlaceMetres, Sk));
        Assert.Equal(0, SkaterAvoidance.OpponentWeight(Sk.OpponentContactMetres, Sk));
        double mid = SkaterAvoidance.OpponentWeight((Sk.OpponentPlaceMetres + Sk.OpponentContactMetres) / 2, Sk);
        Assert.Equal(0.5, mid, 9);
        double distance;
        Assert.Equal(1, SkaterAvoidance.TeammateWeight(new RinkPoint(Sk.SeparationMinMetres, 0), new RinkPoint(0, 0), Sk, out distance));
        Assert.Equal(0, SkaterAvoidance.TeammateWeight(new RinkPoint(2 * Sk.SeparationMinMetres, 0), new RinkPoint(0, 0), Sk, out distance));

        // Closing from 4 × separationMin at 10 × separationMin per second: the closest approach is 0.4 s ahead, inside the anticipation window.
        double early = SkaterAvoidance.TeammateWeight(new RinkPoint(4 * Sk.SeparationMinMetres, 0), new RinkPoint(-10 * Sk.SeparationMinMetres, 0), Sk, out distance);
        Assert.True(early > 0);
        Assert.Equal(0, distance);
    }
}
