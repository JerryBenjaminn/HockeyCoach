using System;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.State
{
    /// <summary>
    /// The states that persist across stoppages within a match (data-schema.md O-1–O-14): lineups, energy, organization,
    /// pressure, familiarity and the offensive-zone time. Owned by the match; a shift segment updates it.
    /// </summary>
    public sealed class GameState
    {
        private readonly LineupState[] _lineups = new LineupState[2];
        private readonly double[] _organization = { 1.0, 1.0 };
        private readonly double[] _pressure = new double[2];
        private readonly double[] _offensiveZoneSeconds = new double[2];

        /// <summary>Creates the state; every player starts at <paramref name="startEnergy"/>.</summary>
        public GameState(LineupState home, LineupState away, double startEnergy)
        {
            _lineups[(int)TeamSide.Home] = home ?? throw new ArgumentNullException(nameof(home));
            _lineups[(int)TeamSide.Away] = away ?? throw new ArgumentNullException(nameof(away));
            Energy = new EnergyState();
            Familiarity = new FamiliarityState();
            foreach (LineupState lineup in _lineups)
            {
                foreach (Skater skater in lineup.Team.Skaters)
                {
                    Energy.Add(skater.Id, startEnergy);
                    Energy.Bench(skater.Id, 0.0);
                }

                Energy.Add(lineup.Goalie.Id, startEnergy);
            }
        }

        /// <summary>Energy of every player.</summary>
        public EnergyState Energy { get; }

        /// <summary>Play uses (O-11).</summary>
        public FamiliarityState Familiarity { get; }

        /// <summary>The lineup of <paramref name="team"/>.</summary>
        public LineupState Lineup(TeamSide team)
        {
            return _lineups[(int)team];
        }

        /// <summary>Organization (0..1) of <paramref name="team"/>'s defence (O-6).</summary>
        public double Organization(TeamSide team)
        {
            return _organization[(int)team];
        }

        /// <summary>Sets the organization, clamped to 0..1.</summary>
        public void SetOrganization(TeamSide team, double value)
        {
            _organization[(int)team] = Clamp01(value);
        }

        /// <summary>Pressure state (0..1) of <paramref name="team"/> (O-9).</summary>
        public double Pressure(TeamSide team)
        {
            return _pressure[(int)team];
        }

        /// <summary>Sets the pressure state, clamped to 0..1.</summary>
        public void SetPressure(TeamSide team, double value)
        {
            _pressure[(int)team] = Clamp01(value);
        }

        /// <summary>Seconds the puck has been in <paramref name="team"/>'s offensive zone (O-14).</summary>
        public double OffensiveZoneSeconds(TeamSide team)
        {
            return _offensiveZoneSeconds[(int)team];
        }

        /// <summary>Adds offensive-zone time.</summary>
        public void AddOffensiveZoneSeconds(TeamSide team, double seconds)
        {
            _offensiveZoneSeconds[(int)team] += seconds;
        }

        private static double Clamp01(double value)
        {
            if (double.IsNaN(value))
            {
                throw new ArgumentException("State values must be numbers.", nameof(value));
            }

            return value < 0.0 ? 0.0 : (value > 1.0 ? 1.0 : value);
        }
    }
}
