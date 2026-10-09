namespace HockeyCoach.Sim.Events
{
    /// <summary>Visitor over the event schema types (one method per type).</summary>
    /// <typeparam name="T">Result type.</typeparam>
    public interface ISimEventVisitor<out T>
    {
        /// <summary>Faceoff (aloitus).</summary>
        T Visit(FaceoffEvent e);

        /// <summary>Controlled zone entry (hallittu alueelletuonti).</summary>
        T Visit(ControlledZoneEntryEvent e);

        /// <summary>Dump-in (kiekko päätyyn).</summary>
        T Visit(DumpInEvent e);

        /// <summary>Pass (syöttö).</summary>
        T Visit(PassEvent e);

        /// <summary>Shot (laukaus).</summary>
        T Visit(ShotEvent e);

        /// <summary>Turnover (kiekonmenetys / riisto).</summary>
        T Visit(TurnoverEvent e);

        /// <summary>Puck battle (kamppailu).</summary>
        T Visit(PuckBattleEvent e);

        /// <summary>Stoppage (pelikatko).</summary>
        T Visit(StoppageEvent e);

        /// <summary>Line change (vaihto).</summary>
        T Visit(LineChangeEvent e);
    }
}
