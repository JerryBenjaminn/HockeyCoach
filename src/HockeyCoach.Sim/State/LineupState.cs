using System;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.State
{
    /// <summary>
    /// A team's lineup in a match (O-1, O-2, D-023): which forward trio and defence pair are on the ice, since when
    /// (period time), and ice time and shift counts per unit. The goalie plays the whole match.
    /// </summary>
    public sealed class LineupState
    {
        private readonly double[] _forwardSeconds;
        private readonly double[] _pairSeconds;
        private readonly int[] _forwardShifts;
        private readonly int[] _pairShifts;

        /// <summary>Creates the lineup with no unit on the ice yet.</summary>
        public LineupState(Team team, Goalie goalie)
        {
            Team = team ?? throw new ArgumentNullException(nameof(team));
            Goalie = goalie ?? throw new ArgumentNullException(nameof(goalie));
            if (team.ForwardLines.Count == 0 || team.DefencePairs.Count == 0)
            {
                throw new ArgumentException("A team needs at least one forward line and one defence pair.", nameof(team));
            }

            _forwardSeconds = new double[team.ForwardLines.Count];
            _pairSeconds = new double[team.DefencePairs.Count];
            _forwardShifts = new int[team.ForwardLines.Count];
            _pairShifts = new int[team.DefencePairs.Count];
            ForwardIndex = -1;
            PairIndex = -1;
        }

        /// <summary>The team.</summary>
        public Team Team { get; }

        /// <summary>The goalie.</summary>
        public Goalie Goalie { get; }

        /// <summary>Index of the trio on the ice, −1 before the first faceoff.</summary>
        public int ForwardIndex { get; private set; }

        /// <summary>Index of the pair on the ice, −1 before the first faceoff.</summary>
        public int PairIndex { get; private set; }

        /// <summary>Period time when the trio came on.</summary>
        public double ForwardSince { get; private set; }

        /// <summary>Period time when the pair came on.</summary>
        public double PairSince { get; private set; }

        /// <summary>The five skaters on the ice.</summary>
        public OnIceSkaters OnIce
        {
            get
            {
                if (ForwardIndex < 0 || PairIndex < 0)
                {
                    throw new InvalidOperationException("No unit on the ice yet.");
                }

                return new OnIceSkaters(Team.ForwardLines[ForwardIndex], Team.DefencePairs[PairIndex]);
            }
        }

        /// <summary>Puts trio <paramref name="index"/> on the ice at <paramref name="time"/>; counts a shift.</summary>
        public void SetForwards(int index, double time)
        {
            if (index < 0 || index >= Team.ForwardLines.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            ForwardIndex = index;
            ForwardSince = time;
            _forwardShifts[index]++;
        }

        /// <summary>Puts pair <paramref name="index"/> on the ice at <paramref name="time"/>; counts a shift.</summary>
        public void SetPair(int index, double time)
        {
            if (index < 0 || index >= Team.DefencePairs.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            PairIndex = index;
            PairSince = time;
            _pairShifts[index]++;
        }

        /// <summary>Restarts the shifts of the units on the ice at <paramref name="time"/> (a new period, O-1); counts new shifts.</summary>
        public void RestartShifts(double time)
        {
            if (ForwardIndex >= 0)
            {
                SetForwards(ForwardIndex, time);
            }

            if (PairIndex >= 0)
            {
                SetPair(PairIndex, time);
            }
        }

        /// <summary>Adds ice time to the units on the ice.</summary>
        public void AddIceTime(double seconds)
        {
            if (ForwardIndex >= 0)
            {
                _forwardSeconds[ForwardIndex] += seconds;
            }

            if (PairIndex >= 0)
            {
                _pairSeconds[PairIndex] += seconds;
            }
        }

        /// <summary>Total ice time of trio <paramref name="index"/>.</summary>
        public double ForwardSeconds(int index)
        {
            return _forwardSeconds[index];
        }

        /// <summary>Total ice time of pair <paramref name="index"/>.</summary>
        public double PairSeconds(int index)
        {
            return _pairSeconds[index];
        }

        /// <summary>Shifts of trio <paramref name="index"/>.</summary>
        public int ForwardShifts(int index)
        {
            return _forwardShifts[index];
        }

        /// <summary>Shifts of pair <paramref name="index"/>.</summary>
        public int PairShifts(int index)
        {
            return _pairShifts[index];
        }
    }
}
