namespace HockeyCoach.Sim.Shift
{
    /// <summary>The units a coach wants on the ice: forward trio and defence pair indices into the team's lines (O-2, O-4).</summary>
    public sealed class LineChoice
    {
        /// <summary>Creates the choice.</summary>
        public LineChoice(int forwardIndex, int pairIndex)
        {
            ForwardIndex = forwardIndex;
            PairIndex = pairIndex;
        }

        /// <summary>Index of the forward trio.</summary>
        public int ForwardIndex { get; }

        /// <summary>Index of the defence pair.</summary>
        public int PairIndex { get; }
    }
}
