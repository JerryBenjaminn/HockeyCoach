using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.State
{
    /// <summary>
    /// Energy (0..1) of every player by id, and when each benched skater left the ice (period time) for the closed-form
    /// bench recovery (O-5). Iteration is in id order.
    /// </summary>
    public sealed class EnergyState
    {
        private readonly SortedDictionary<int, double> _energy = new SortedDictionary<int, double>();
        private readonly SortedDictionary<int, double> _benchSince = new SortedDictionary<int, double>();

        /// <summary>All player ids, in id order.</summary>
        public IEnumerable<int> Ids
        {
            get { return _energy.Keys; }
        }

        /// <summary>Adds a player with a starting energy.</summary>
        public void Add(int id, double energy)
        {
            _energy.Add(id, Clamp(energy));
        }

        /// <summary>The stored energy (for a benched skater, as of leaving the ice).</summary>
        public double Get(int id)
        {
            if (_energy.TryGetValue(id, out double value))
            {
                return value;
            }

            throw new KeyNotFoundException("No energy for player " + id + ".");
        }

        /// <summary>Sets the energy, clamped to 0..1.</summary>
        public void Set(int id, double energy)
        {
            if (!_energy.ContainsKey(id))
            {
                throw new KeyNotFoundException("No energy for player " + id + ".");
            }

            _energy[id] = Clamp(energy);
        }

        /// <summary>Marks a skater benched at <paramref name="time"/>.</summary>
        public void Bench(int id, double time)
        {
            _benchSince[id] = time;
        }

        /// <summary>Whether the skater is benched; <paramref name="since"/> is when he left the ice.</summary>
        public bool IsBenched(int id, out double since)
        {
            return _benchSince.TryGetValue(id, out since);
        }

        /// <summary>Marks a skater back on the ice.</summary>
        public void Unbench(int id)
        {
            _benchSince.Remove(id);
        }

        /// <summary>Every player to <paramref name="energy"/>; benched skaters restart their bench time at <paramref name="time"/>.</summary>
        public void ResetAll(double energy, double time)
        {
            var ids = new List<int>(_energy.Keys);
            foreach (int id in ids)
            {
                _energy[id] = Clamp(energy);
            }

            var benched = new List<int>(_benchSince.Keys);
            foreach (int id in benched)
            {
                _benchSince[id] = time;
            }
        }

        private static double Clamp(double value)
        {
            if (double.IsNaN(value))
            {
                throw new ArgumentException("Energy must be a number.", nameof(value));
            }

            return value < 0.0 ? 0.0 : (value > 1.0 ? 1.0 : value);
        }
    }
}
