namespace HockeyCoach.Sim.Events
{
    /// <summary>How a shot chance was created (docs/stats-and-checks.md, Paikkatyypit).</summary>
    public enum ChanceType
    {
        /// <summary>Rush before the defence organized (suorahyökkäys).</summary>
        Rush = 0,

        /// <summary>Soon after a takeaway (kiekonriisto).</summary>
        Turnover = 1,

        /// <summary>Offensive-zone play against an organized defence (alueella pelaaminen).</summary>
        OffensiveZone = 2,

        /// <summary>Power play or penalty kill (ylivoima / alivoima).</summary>
        SpecialTeams = 3,

        /// <summary>Directly from a faceoff play (aloitus).</summary>
        Faceoff = 4,
    }
}
