using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tests.Support;

/// <summary>Hand-built rinks with the documented 11 × 5 layout (data-schema.md), independent of data/rink.json.</summary>
internal static class TestRinks
{
    /// <summary>Faceoff spots of data-schema.md (Aloituspisteet).</summary>
    public static FaceoffSpot[] StandardSpots() => new[]
    {
        new FaceoffSpot("defensiveLeft", new GridPoint(2, 1)),
        new FaceoffSpot("defensiveRight", new GridPoint(2, 3)),
        new FaceoffSpot("neutralDefensiveLeft", new GridPoint(4, 1)),
        new FaceoffSpot("neutralDefensiveRight", new GridPoint(4, 3)),
        new FaceoffSpot("center", new GridPoint(5, 2)),
        new FaceoffSpot("neutralOffensiveLeft", new GridPoint(6, 1)),
        new FaceoffSpot("neutralOffensiveRight", new GridPoint(6, 3)),
        new FaceoffSpot("offensiveLeft", new GridPoint(8, 1)),
        new FaceoffSpot("offensiveRight", new GridPoint(8, 3)),
    };

    /// <summary>
    /// 11 × 5: goals (1, 2) and (9, 2), zones defensive 0–3, neutral 4–6, offensive 7–10, slot (7, 2) and (8, 2),
    /// xG zone "slot" on slot nodes and "longRange" elsewhere.
    /// </summary>
    public static Rink Standard(FaceoffSpot[]? spots = null, Func<int, int, bool>? isSlot = null)
    {
        var nodes = new List<RinkNode>();
        for (int x = 0; x < 11; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                bool slot = isSlot != null ? isSlot(x, y) : y == 2 && (x == 7 || x == 8);
                nodes.Add(new RinkNode(x, y, slot ? "slot" : "longRange", slot));
            }
        }

        var zones = new[]
        {
            new ZoneRange(RinkZone.Defensive, 0, 3),
            new ZoneRange(RinkZone.Neutral, 4, 6),
            new ZoneRange(RinkZone.Offensive, 7, 10),
        };
        return new Rink(11, 5, nodes, zones, new[] { "slot", "longRange" }, new GridPoint(1, 2), new GridPoint(9, 2), spots ?? StandardSpots());
    }
}
