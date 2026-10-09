namespace HockeyCoach.Sim.Model
{
    /// <summary>The 12 skater stats (docs/stats-and-checks.md). Values are on the internal 1–20 scale.</summary>
    public enum SkaterStat
    {
        /// <summary>Skating speed (data name <c>speed</c>).</summary>
        Speed = 0,

        /// <summary>Agility (data name <c>agility</c>).</summary>
        Agility = 1,

        /// <summary>Endurance (data name <c>endurance</c>).</summary>
        Endurance = 2,

        /// <summary>Hands (data name <c>hands</c>).</summary>
        Hands = 3,

        /// <summary>Passing (data name <c>passing</c>).</summary>
        Passing = 4,

        /// <summary>Shot accuracy (data name <c>shotAccuracy</c>).</summary>
        ShotAccuracy = 5,

        /// <summary>Shot power (data name <c>shotPower</c>).</summary>
        ShotPower = 6,

        /// <summary>Defensive positioning (data name <c>positioning</c>).</summary>
        Positioning = 7,

        /// <summary>Awareness / reading the game (data name <c>awareness</c>).</summary>
        Awareness = 8,

        /// <summary>Strength (data name <c>strength</c>).</summary>
        Strength = 9,

        /// <summary>Discipline (data name <c>discipline</c>).</summary>
        Discipline = 10,

        /// <summary>Faceoffs (data name <c>faceoffs</c>).</summary>
        Faceoffs = 11,
    }
}
