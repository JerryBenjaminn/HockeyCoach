namespace HockeyCoach.Sim.Config
{
    /// <summary>Kind of a check (tuning.json <c>checks.&lt;id&gt;.kind</c>, D-017 / D-019).</summary>
    public enum CheckKind
    {
        /// <summary><c>twoSided</c>: attacker weighted stats vs defender weighted stats.</summary>
        TwoSided = 0,

        /// <summary><c>oneSided</c>: one side's weighted stats vs <see cref="CheckFormulaConfig.ReferenceValue"/>.</summary>
        OneSided = 1,

        /// <summary><c>noCheck</c>: no roll; only holds values used by other checks (e.g. <c>dumpIn</c>).</summary>
        NoCheck = 2,
    }
}
