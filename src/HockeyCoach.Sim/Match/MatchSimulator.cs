using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.State;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Match
{
    /// <summary>
    /// Plays a match (data-schema.md O-1–O-4): periods of stoppage-to-stoppage segments, coaches at every stoppage and
    /// period start, stoppage changes, faceoff spots, pressure decay at stoppages and the intermission resets. A tie is
    /// allowed (D-062). Deterministic for the same seed, coaches and data.
    /// </summary>
    public static class MatchSimulator
    {
        /// <summary>Runs the match.</summary>
        public static MatchResult Run(MatchSetup setup, ICoach home, ICoach away, IRandom random)
        {
            if (setup == null)
            {
                throw new ArgumentNullException(nameof(setup));
            }

            if (home == null || away == null)
            {
                throw new ArgumentNullException(home == null ? nameof(home) : nameof(away));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            return new Game(setup, new[] { home, away }, random).Play();
        }

        private sealed class Game : IShiftHost
        {
            private readonly MatchSetup _setup;
            private readonly TuningConfig _tuning;
            private readonly ICoach[] _coaches;
            private readonly IRandom _random;
            private readonly GameState _state;
            private readonly EventLog _log = new EventLog();
            private readonly ShiftPlan[] _plans = new ShiftPlan[2];
            private readonly int[] _goals = new int[2];
            private readonly List<PeriodSnapshot> _periods = new List<PeriodSnapshot>();
            private int _period;

            internal Game(MatchSetup setup, ICoach[] coaches, IRandom random)
            {
                _setup = setup;
                _tuning = setup.Tuning;
                _coaches = coaches;
                _random = random;
                _state = new GameState(
                    new LineupState(setup.Home, setup.Home.Goalies[0]),
                    new LineupState(setup.Away, setup.Away.Goalies[0]),
                    _tuning.Energy.Start);
            }

            public LineChoice OnTheFly(TeamSide team, bool forwardsEligible, bool defenceEligible, double time)
            {
                return _coaches[(int)team].OnTheFly(View(team, time, forwardsEligible, defenceEligible));
            }

            internal MatchResult Play()
            {
                bool stalled = false;
                int segments = 0;
                for (_period = 1; _period <= _tuning.Time.Periods && !stalled; _period++)
                {
                    if (_period > 1)
                    {
                        Intermission();
                    }

                    double time = 0.0;
                    string spot = FaceoffSpots.Center;
                    LineChoice[] units = Decide(time, true);
                    while (true)
                    {
                        segments++;
                        if (segments > _setup.MaxSegments)
                        {
                            stalled = true;
                            break;
                        }

                        ShiftResult result = RunSegment(spot, time, units);
                        _goals[(int)TeamSide.Home] += result.HomeGoals;
                        _goals[(int)TeamSide.Away] += result.AwayGoals;
                        time = result.EndTime;
                        if (result.EndReason == ShiftEndReason.StepCap)
                        {
                            stalled = true;
                            break;
                        }

                        if (result.EndReason == ShiftEndReason.PeriodEnd)
                        {
                            break;
                        }

                        // O-13: every stoppage keeps a share of the pressure; commitments end with the segment.
                        _state.SetPressure(TeamSide.Home, _state.Pressure(TeamSide.Home) * _tuning.Pressure.KeepOnStoppage);
                        _state.SetPressure(TeamSide.Away, _state.Pressure(TeamSide.Away) * _tuning.Pressure.KeepOnStoppage);
                        spot = FaceoffSpots.After(_setup.Rink, result);
                        units = Decide(time, false);
                    }

                    if (!stalled)
                    {
                        _periods.Add(Snapshot(_tuning.Time.PeriodSeconds));
                    }
                }

                return new MatchResult(_log, _goals[(int)TeamSide.Home], _goals[(int)TeamSide.Away], stalled, _periods, _state);
            }

            /// <summary>O-1 intermission: energy to start, pressure 0, organization 1, play uses × intermissionMultiplier.</summary>
            private void Intermission()
            {
                _state.Energy.ResetAll(_tuning.Energy.Start, 0.0);
                foreach (TeamSide team in new[] { TeamSide.Home, TeamSide.Away })
                {
                    _state.SetPressure(team, 0.0);
                    _state.SetOrganization(team, 1.0);
                }

                _state.Familiarity.Multiply(_tuning.Familiarity.IntermissionMultiplier);
            }

            /// <summary>Asks both coaches; at a period start every unit may change and kept units start a new shift.</summary>
            private LineChoice[] Decide(double time, bool periodStart)
            {
                var units = new LineChoice[2];
                foreach (TeamSide team in new[] { TeamSide.Home, TeamSide.Away })
                {
                    LineupState lineup = _state.Lineup(team);
                    double minimum = _tuning.Time.StoppageChangeMinSeconds;
                    bool forwards = periodStart || time - lineup.ForwardSince >= minimum - 1e-9;
                    bool defence = periodStart || time - lineup.PairSince >= minimum - 1e-9;
                    CoachDecision decision = _coaches[(int)team].AtStoppage(View(team, time, forwards, defence));
                    if (decision.Units.ForwardIndex != lineup.ForwardIndex && !forwards)
                    {
                        throw new InvalidOperationException("The coach changed a forward trio that is not eligible at this stoppage (O-2).");
                    }

                    if (decision.Units.PairIndex != lineup.PairIndex && !defence)
                    {
                        throw new InvalidOperationException("The coach changed a defence pair that is not eligible at this stoppage (O-2).");
                    }

                    if (periodStart && _period > 1)
                    {
                        if (decision.Units.ForwardIndex == lineup.ForwardIndex)
                        {
                            lineup.SetForwards(lineup.ForwardIndex, time);
                        }

                        if (decision.Units.PairIndex == lineup.PairIndex)
                        {
                            lineup.SetPair(lineup.PairIndex, time);
                        }
                    }

                    _plans[(int)team] = decision.Plan;
                    units[(int)team] = decision.Units;
                }

                return units;
            }

            private ShiftResult RunSegment(string spot, double time, LineChoice[] units)
            {
                var context = new ShiftContext(_state, _log, this, units[(int)TeamSide.Home], units[(int)TeamSide.Away]);
                var setup = new ShiftSetup(
                    _setup.Rink,
                    _tuning,
                    TeamSetup(TeamSide.Home, units[(int)TeamSide.Home]),
                    TeamSetup(TeamSide.Away, units[(int)TeamSide.Away]),
                    spot,
                    _period,
                    time,
                    _setup.MaxStepsPerSegment,
                    context);
                return ShiftSimulator.Run(setup, _random);
            }

            private TeamShiftSetup TeamSetup(TeamSide team, LineChoice units)
            {
                LineupState lineup = _state.Lineup(team);
                var onIce = new OnIceSkaters(lineup.Team.ForwardLines[units.ForwardIndex], lineup.Team.DefencePairs[units.PairIndex]);
                return new TeamShiftSetup(onIce, lineup.Goalie, _plans[(int)team]);
            }

            private CoachView View(TeamSide team, double time, bool forwards, bool defence)
            {
                LineupState lineup = _state.Lineup(team);
                return new CoachView(
                    team,
                    lineup.Team,
                    _period,
                    time,
                    _goals[(int)team],
                    _goals[(int)TeamSides.Opponent(team)],
                    lineup.ForwardIndex,
                    lineup.PairIndex,
                    forwards,
                    defence,
                    new SortedDictionary<string, double>(ToDictionary(_state.Familiarity.UsesOf(team)), StringComparer.Ordinal),
                    TeamEnergy(lineup, time));
            }

            private static IDictionary<string, double> ToDictionary(IReadOnlyDictionary<string, double> source)
            {
                var copy = new Dictionary<string, double>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, double> entry in source)
                {
                    copy.Add(entry.Key, entry.Value);
                }

                return copy;
            }

            /// <summary>Energy of the team's players as of <paramref name="time"/>, bench recovery applied.</summary>
            private SortedDictionary<int, double> TeamEnergy(LineupState lineup, double time)
            {
                var energy = new SortedDictionary<int, double>();
                foreach (Skater skater in lineup.Team.Skaters)
                {
                    energy[skater.Id] = CurrentEnergy(skater.Id, time);
                }

                energy[lineup.Goalie.Id] = _state.Energy.Get(lineup.Goalie.Id);
                return energy;
            }

            private double CurrentEnergy(int id, double time)
            {
                double energy = _state.Energy.Get(id);
                return _state.Energy.IsBenched(id, out double since)
                    ? StateDynamics.BenchRecovered(energy, time - since, _tuning.Energy.BenchRecoveryRate)
                    : energy;
            }

            private PeriodSnapshot Snapshot(double time)
            {
                var energy = new SortedDictionary<int, double>();
                foreach (int id in _state.Energy.Ids)
                {
                    energy[id] = CurrentEnergy(id, time);
                }

                var forwardSeconds = new double[2][];
                var pairSeconds = new double[2][];
                var forwardShifts = new int[2][];
                var pairShifts = new int[2][];
                var zone = new double[2];
                var uses = new IReadOnlyDictionary<string, double>[2];
                foreach (TeamSide team in new[] { TeamSide.Home, TeamSide.Away })
                {
                    LineupState lineup = _state.Lineup(team);
                    int t = (int)team;
                    forwardSeconds[t] = new double[lineup.Team.ForwardLines.Count];
                    forwardShifts[t] = new int[lineup.Team.ForwardLines.Count];
                    for (int i = 0; i < forwardSeconds[t].Length; i++)
                    {
                        forwardSeconds[t][i] = lineup.ForwardSeconds(i);
                        forwardShifts[t][i] = lineup.ForwardShifts(i);
                    }

                    pairSeconds[t] = new double[lineup.Team.DefencePairs.Count];
                    pairShifts[t] = new int[lineup.Team.DefencePairs.Count];
                    for (int i = 0; i < pairSeconds[t].Length; i++)
                    {
                        pairSeconds[t][i] = lineup.PairSeconds(i);
                        pairShifts[t][i] = lineup.PairShifts(i);
                    }

                    zone[t] = _state.OffensiveZoneSeconds(team);
                    uses[t] = new SortedDictionary<string, double>(ToDictionary(_state.Familiarity.UsesOf(team)), StringComparer.Ordinal);
                }

                return new PeriodSnapshot(_period, energy, forwardSeconds, pairSeconds, forwardShifts, pairShifts, zone, uses);
            }
        }
    }
}
