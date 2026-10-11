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
        /// <param name="setup">Rink, tuning, teams and caps.</param>
        /// <param name="home">Home coach (holds its own random stream if it needs one, D-077).</param>
        /// <param name="away">Away coach.</param>
        /// <param name="random">The simulation stream, e.g. <see cref="MatchStreams.Simulation"/>; coaches never draw from it.</param>
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
            private readonly int[] _timeoutsUsed = new int[2];
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
                return _coaches[(int)team].OnTheFly(View(team, time, forwardsEligible, defenceEligible, CoachCall.OnTheFly, null));
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
                    LineChoice[] units = Decide(time, true, null);
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
                        units = Decide(time, false, Reason(result.EndReason));
                    }

                    if (!stalled)
                    {
                        _periods.Add(Snapshot(_tuning.Time.PeriodSeconds));
                    }
                }

                return new MatchResult(_log, _goals[(int)TeamSide.Home], _goals[(int)TeamSide.Away], stalled, _periods, _state);
            }

            private static StoppageReason Reason(ShiftEndReason end)
            {
                switch (end)
                {
                    case ShiftEndReason.Goal:
                        return StoppageReason.Goal;
                    case ShiftEndReason.GoalieFreeze:
                        return StoppageReason.GoalieFreeze;
                    case ShiftEndReason.OutOfPlay:
                        return StoppageReason.OutOfPlay;
                    default:
                        throw new InvalidOperationException("No stoppage for segment end " + end + ".");
                }
            }

            /// <summary>O-1 intermission: energy to start, pressure 0, organization 1, play uses × intermissionMultiplier; bench clocks restart.</summary>
            private void Intermission()
            {
                _state.Energy.ResetAll(_tuning.Energy.Start, 0.0);
                foreach (TeamSide team in new[] { TeamSide.Home, TeamSide.Away })
                {
                    _state.SetPressure(team, 0.0);
                    _state.SetOrganization(team, 1.0);
                }

                _state.Familiarity.Multiply(_tuning.Familiarity.IntermissionMultiplier);
                _state.Lineup(TeamSide.Home).StartPeriodBenchClocks();
                _state.Lineup(TeamSide.Away).StartPeriodBenchClocks();
            }

            /// <summary>
            /// Asks both coaches; at a period start every unit may change and kept units start a new shift. Decisions are
            /// simultaneous (D-077): both views are made before either coach is asked, and no decision is applied until
            /// both coaches have answered.
            /// </summary>
            private LineChoice[] Decide(double time, bool periodStart, StoppageReason? reason)
            {
                var units = new LineChoice[2];
                var forwardsEligible = new bool[2];
                var defenceEligible = new bool[2];
                foreach (TeamSide team in new[] { TeamSide.Home, TeamSide.Away })
                {
                    LineupState lineup = _state.Lineup(team);
                    double minimum = _tuning.Time.StoppageChangeMinSeconds;
                    forwardsEligible[(int)team] = periodStart || time - lineup.ForwardSince >= minimum - 1e-9;
                    defenceEligible[(int)team] = periodStart || time - lineup.PairSince >= minimum - 1e-9;
                }

                CoachCall call = periodStart ? CoachCall.PeriodStart : CoachCall.Stoppage;
                StoppageReason? stoppage = periodStart ? null : reason;
                CoachView[] views = Views(time, forwardsEligible, defenceEligible, call, stoppage, false);
                CoachDecision[] decisions = Ask(views);
                bool[] called = new bool[2];
                foreach (TeamSide team in new[] { TeamSide.Home, TeamSide.Away })
                {
                    if (decisions[(int)team].CallTimeout && !views[(int)team].Timeouts.CanCall)
                    {
                        throw new InvalidOperationException("The coach called a timeout that is not allowed now (AI-4, AI-5).");
                    }

                    called[(int)team] = decisions[(int)team].CallTimeout;
                }

                if (called[(int)TeamSide.Home] || called[(int)TeamSide.Away])
                {
                    // AI-4: log and apply every timeout (home first), then ask both coaches again; that round replaces the first.
                    foreach (TeamSide team in new[] { TeamSide.Home, TeamSide.Away })
                    {
                        if (called[(int)team])
                        {
                            CallTimeout(team, time);
                        }
                    }

                    views = Views(time, forwardsEligible, defenceEligible, call, stoppage, true);
                    decisions = Ask(views);
                    foreach (CoachDecision decision in decisions)
                    {
                        if (decision.CallTimeout)
                        {
                            throw new InvalidOperationException("The coach called a timeout in the round after a timeout (AI-4).");
                        }
                    }
                }

                foreach (TeamSide team in new[] { TeamSide.Home, TeamSide.Away })
                {
                    LineupState lineup = _state.Lineup(team);
                    bool forwards = forwardsEligible[(int)team];
                    bool defence = defenceEligible[(int)team];
                    CoachDecision decision = decisions[(int)team];
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

            /// <summary>Both coaches' views, made before either coach is asked (simultaneous decisions, D-077).</summary>
            private CoachView[] Views(double time, bool[] forwards, bool[] defence, CoachCall call, StoppageReason? reason, bool afterTimeout)
            {
                var views = new CoachView[2];
                foreach (TeamSide team in new[] { TeamSide.Home, TeamSide.Away })
                {
                    views[(int)team] = View(team, time, forwards[(int)team], defence[(int)team], call, reason, afterTimeout);
                }

                return views;
            }

            private CoachDecision[] Ask(CoachView[] views)
            {
                var decisions = new CoachDecision[2];
                foreach (TeamSide team in new[] { TeamSide.Home, TeamSide.Away })
                {
                    decisions[(int)team] = _coaches[(int)team].AtStoppage(views[(int)team])
                        ?? throw new InvalidOperationException("The coach returned no decision.");
                }

                return decisions;
            }

            /// <summary>
            /// AI-4: logs the timeout at the stoppage and rests every skater of both teams for <c>time.timeoutRestSeconds</c>
            /// (bench recovery is realized first). The clock, ice time, eligibility, goalies, pressure, organization and
            /// familiarity do not change.
            /// </summary>
            private void CallTimeout(TeamSide team, double time)
            {
                SimEvent last = _log.Count > 0 ? _log.Events[_log.Count - 1] : null;
                if (!(last is StoppageEvent) && !(last is TimeoutEvent))
                {
                    throw new InvalidOperationException("A timeout must follow a stoppage.");
                }

                _log.Append(new TimeoutEvent(last.Context, team));
                _timeoutsUsed[(int)team]++;
                foreach (TeamSide side in new[] { TeamSide.Home, TeamSide.Away })
                {
                    TimeoutRest.Apply(_state.Energy, _state.Lineup(side).Team.Skaters, time, _tuning.Energy.BenchRecoveryRate, _tuning.Time.TimeoutRestSeconds);
                }
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

            private CoachView View(TeamSide team, double time, bool forwards, bool defence, CoachCall call, StoppageReason? reason)
            {
                return View(team, time, forwards, defence, call, reason, false);
            }

            private int TimeoutsLeft(TeamSide team)
            {
                return Math.Max(0, _tuning.Time.TimeoutsPerTeamPerMatch - _timeoutsUsed[(int)team]);
            }

            private CoachView View(TeamSide team, double time, bool forwards, bool defence, CoachCall call, StoppageReason? reason, bool afterTimeout)
            {
                int ownLeft = TimeoutsLeft(team);
                bool canCall = call == CoachCall.Stoppage && reason.HasValue && CoachTimeouts.ReasonAllows(reason.Value) && !afterTimeout && ownLeft > 0;
                var timeouts = new CoachTimeouts(ownLeft, TimeoutsLeft(TeamSides.Opponent(team)), afterTimeout, canCall);
                LineupState lineup = _state.Lineup(team);
                LineupState opponent = _state.Lineup(TeamSides.Opponent(team));
                return new CoachView(
                    team,
                    lineup.Team,
                    new CoachClock(_period, time, _tuning.Time.Periods, _tuning.Time.PeriodSeconds),
                    call,
                    reason,
                    _goals[(int)team],
                    _goals[(int)TeamSides.Opponent(team)],
                    new LineChoice(lineup.ForwardIndex, lineup.PairIndex),
                    new LineChoice(opponent.ForwardIndex, opponent.PairIndex),
                    forwards,
                    defence,
                    IceTime(lineup, time),
                    timeouts,
                    _state.Familiarity.UsesOf(team),
                    _log.Snapshot());
            }

            /// <summary>
            /// Own ice and bench time. Units still on the ice from the previous period at a period start count 0 s of
            /// shift; bench time is measured within the period.
            /// </summary>
            private static IceTimeView IceTime(LineupState lineup, double time)
            {
                var trios = new UnitIceTime[lineup.Team.ForwardLines.Count];
                for (int i = 0; i < trios.Length; i++)
                {
                    bool on = i == lineup.ForwardIndex;
                    trios[i] = new UnitIceTime(
                        lineup.ForwardSeconds(i),
                        lineup.ForwardShifts(i),
                        on,
                        on ? Math.Max(0.0, time - lineup.ForwardSince) : 0.0,
                        on ? 0.0 : Math.Max(0.0, time - lineup.ForwardOffAt(i)));
                }

                var pairs = new UnitIceTime[lineup.Team.DefencePairs.Count];
                for (int i = 0; i < pairs.Length; i++)
                {
                    bool on = i == lineup.PairIndex;
                    pairs[i] = new UnitIceTime(
                        lineup.PairSeconds(i),
                        lineup.PairShifts(i),
                        on,
                        on ? Math.Max(0.0, time - lineup.PairSince) : 0.0,
                        on ? 0.0 : Math.Max(0.0, time - lineup.PairOffAt(i)));
                }

                return new IceTimeView(trios, pairs);
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
