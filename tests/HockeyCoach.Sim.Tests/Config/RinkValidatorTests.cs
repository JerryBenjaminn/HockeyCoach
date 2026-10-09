using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Config;

public class RinkValidatorTests
{
    [Fact]
    public void GridRink_IsValid()
    {
        Assert.Empty(RinkValidator.Validate(TestPlayers.GridRink(9, 5)));
    }

    [Fact]
    public void Rink_IsInvalid_WhenZonesLeaveColumnUncovered()
    {
        Rink rink = Build(zones: new[] { new ZoneRange(RinkZone.Defensive, 0, 0), new ZoneRange(RinkZone.Offensive, 2, 2) });

        Assert.Contains("zones: x 1 belongs to 0 zones, expected exactly 1", RinkValidator.Validate(rink));
    }

    [Fact]
    public void Rink_IsInvalid_WhenNodeZoneIsNotListed()
    {
        Rink rink = Build(zones: new[] { new ZoneRange(RinkZone.Defensive, 0, 2) });

        Assert.Contains(RinkValidator.Validate(rink), e => e.StartsWith("nodes[", StringComparison.Ordinal) && e.Contains(".zone", StringComparison.Ordinal));
    }

    [Fact]
    public void Rink_IsInvalid_WhenNodeXgZoneIsNotListed()
    {
        Rink rink = Build(xgZones: new[] { "other" });

        Assert.Contains("nodes[0].xgZone: none is not listed in xgZones", RinkValidator.Validate(rink));
    }

    [Fact]
    public void Rink_IsInvalid_WhenFaceoffSpotIsOutsideOrDuplicated()
    {
        Rink rink = Build(spots: new[] { new FaceoffSpot("a", new GridPoint(1, 0)), new FaceoffSpot("a", new GridPoint(5, 0)) });

        IReadOnlyList<string> errors = RinkValidator.Validate(rink);

        Assert.Contains("faceoffSpots[1].id: duplicate faceoff spot id a", errors);
        Assert.Contains(errors, e => e.StartsWith("faceoffSpots[1]: (5,0) is outside", StringComparison.Ordinal));
    }

    private static Rink Build(ZoneRange[]? zones = null, string[]? xgZones = null, FaceoffSpot[]? spots = null)
    {
        var nodes = new List<RinkNode>();
        RinkZone[] byX = { RinkZone.Defensive, RinkZone.Neutral, RinkZone.Offensive };
        for (int x = 0; x < 3; x++)
        {
            nodes.Add(new RinkNode(x, 0, byX[x], "none", false));
        }

        return new Rink(
            3,
            1,
            nodes,
            zones ?? new[] { new ZoneRange(RinkZone.Defensive, 0, 0), new ZoneRange(RinkZone.Neutral, 1, 1), new ZoneRange(RinkZone.Offensive, 2, 2) },
            xgZones ?? new[] { "none" },
            new GridPoint(0, 0),
            new GridPoint(2, 0),
            spots ?? Array.Empty<FaceoffSpot>());
    }
}
