using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Config;

/// <summary>Rink rules for mirroring and goal nodes (data-schema.md, rink.json → Validointi; D-033, D-034).</summary>
public class RinkSymmetryValidatorTests
{
    [Fact]
    public void StandardRink_IsValid()
    {
        Assert.Empty(RinkValidator.Validate(TestRinks.Standard()));
    }

    [Fact]
    public void GoalOffTheMiddleLane_AndNotRotationImages_AreRejected()
    {
        var nodes = new List<RinkNode>();
        for (int x = 0; x < 11; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                nodes.Add(new RinkNode(x, y, "none", false));
            }
        }

        var zones = new[] { new ZoneRange(RinkZone.Defensive, 0, 3), new ZoneRange(RinkZone.Neutral, 4, 6), new ZoneRange(RinkZone.Offensive, 7, 10) };
        var rink = new Rink(11, 5, nodes, zones, new[] { "none" }, new GridPoint(1, 2), new GridPoint(9, 1), Array.Empty<FaceoffSpot>());

        IReadOnlyList<string> errors = RinkValidator.Validate(rink);

        Assert.Contains("opponentGoal: (9,1) is not on the middle lane y = (width - 1) / 2", errors);
        Assert.Contains("opponentGoal: (9,1) is not the 180° rotation of ownGoal (1,2)", errors);
    }

    [Fact]
    public void SlotGoalNode_IsRejected()
    {
        Rink rink = TestRinks.Standard(isSlot: (x, y) => y == 2 && x >= 7 && x <= 9);

        Assert.Contains("opponentGoal: goal node (9,2) must have isSlot false (D-033)", RinkValidator.Validate(rink));
    }

    [Fact]
    public void AsymmetricSlot_IsRejected()
    {
        Rink rink = TestRinks.Standard(isSlot: (x, y) => (y == 2 && (x == 7 || x == 8)) || (x == 8 && y == 1));

        Assert.Contains(RinkValidator.Validate(rink), e => e.StartsWith("nodes[41]: (8,1) and its mirror (8,3) must have the same", StringComparison.Ordinal));
    }

    [Fact]
    public void FaceoffSpotOnGoal_IsRejected()
    {
        FaceoffSpot[] spots = TestRinks.StandardSpots().Append(new FaceoffSpot("goal", new GridPoint(9, 2))).ToArray();

        Assert.Contains("opponentGoal: goal node (9,2) must not be a faceoff spot (goal)", RinkValidator.Validate(TestRinks.Standard(spots)));
    }

    [Fact]
    public void LeftSpotWithoutMirrorPartner_IsRejected()
    {
        FaceoffSpot[] missing = TestRinks.StandardSpots().Where(s => s.Id != "offensiveRight").ToArray();
        FaceoffSpot[] moved = TestRinks.StandardSpots().Select(s => s.Id == "offensiveRight" ? new FaceoffSpot(s.Id, new GridPoint(7, 3)) : s).ToArray();

        Assert.Contains("faceoffSpots[7].id: offensiveLeft has no mirror partner offensiveRight", RinkValidator.Validate(TestRinks.Standard(missing)));
        Assert.Contains(RinkValidator.Validate(TestRinks.Standard(moved)), e => e.StartsWith("faceoffSpots[7]: offensiveLeft (8,1) and offensiveRight (7,3) are not mirror images", StringComparison.Ordinal));
    }

    [Fact]
    public void MiddleLaneSpotNamedLeftOrRight_IsRejected()
    {
        FaceoffSpot[] spots = TestRinks.StandardSpots().Select(s => s.Id == "center" ? new FaceoffSpot("centerLeft", s.Point) : s).ToArray();

        Assert.Contains(RinkValidator.Validate(TestRinks.Standard(spots)), e => e.StartsWith("faceoffSpots[4].id: centerLeft is on the middle lane", StringComparison.Ordinal));
    }
}
