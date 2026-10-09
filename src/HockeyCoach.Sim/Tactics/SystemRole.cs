namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// Defensive system role (data-schema.md, Puolustusjärjestelmät). F1–F3 go to the forward trio and D1–D2 to the
    /// defence pair by distance to the puck (Q-011). A role is not a position.
    /// </summary>
    public enum SystemRole
    {
        /// <summary>Forward closest to the puck.</summary>
        F1 = 0,

        /// <summary>Second-closest forward.</summary>
        F2 = 1,

        /// <summary>Third forward.</summary>
        F3 = 2,

        /// <summary>Defenceman closest to the puck.</summary>
        D1 = 3,

        /// <summary>The other defenceman.</summary>
        D2 = 4,
    }
}
