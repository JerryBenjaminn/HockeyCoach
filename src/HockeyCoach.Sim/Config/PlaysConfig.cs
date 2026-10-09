namespace HockeyCoach.Sim.Config
{
    /// <summary>Play limits from tuning.json <c>plays</c> (docs/data-schema.md, Kuviot).</summary>
    public sealed class PlaysConfig
    {
        /// <summary>Creates the plays config. Use <see cref="TuningValidator"/> to check it.</summary>
        /// <param name="maxBeats">Most beats a play may have.</param>
        /// <param name="maxNodesPerBeat">Most nodes (Chebyshev) a player moves per beat or event.</param>
        public PlaysConfig(int maxBeats, int maxNodesPerBeat)
        {
            MaxBeats = maxBeats;
            MaxNodesPerBeat = maxNodesPerBeat;
        }

        /// <summary>Most beats a play may have.</summary>
        public int MaxBeats { get; }

        /// <summary>
        /// Most nodes (Chebyshev) a move, a skate or a driveNet covers in one beat; also the most steps a defender
        /// takes toward its system target per event.
        /// </summary>
        public int MaxNodesPerBeat { get; }
    }
}
