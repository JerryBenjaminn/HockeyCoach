using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.State;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// Line changes (O-2, D-023): stoppage changes before the faceoff formation and on-the-fly changes of the team with
    /// the puck at a safe moment. Incoming skaters take the outgoing skaters' nodes; changes cost no time and do not touch
    /// organization. One Vaihto event per unit, trio before pair, home before away.
    /// </summary>
    internal sealed partial class ShiftRun
    {
        private readonly List<KeyValuePair<int[], int[]>> _pendingChanges = new List<KeyValuePair<int[], int[]>>();

        /// <summary>A standalone segment (the <c>shift</c> command): fresh state with the setup's on-ice units as the whole lineup.</summary>
        private static ShiftContext StandaloneContext(ShiftSetup setup)
        {
            LineupState home = StandaloneLineup("Home", setup.Home);
            LineupState away = StandaloneLineup("Away", setup.Away);
            var state = new GameState(home, away, setup.Tuning.Energy.Start);
            return new ShiftContext(state, new EventLog(), null, new LineChoice(0, 0), new LineChoice(0, 0));
        }

        private static LineupState StandaloneLineup(string name, TeamShiftSetup team)
        {
            OnIceSkaters unit = team.Skaters;
            var roster = new Team(name, unit.Skaters, new[] { team.Goalie }, new[] { unit.Forwards }, new[] { unit.Defence });
            return new LineupState(roster, team.Goalie);
        }

        /// <summary>Stoppage changes (O-2): bookkeeping now, events once the faceoff formation is placed.</summary>
        private void ApplyStartUnits(ShiftContext context)
        {
            foreach (TeamSide team in new[] { TeamSide.Home, TeamSide.Away })
            {
                LineupState lineup = _game.Lineup(team);
                LineChoice choice = context.StartUnits(team);
                if (choice.ForwardIndex != lineup.ForwardIndex)
                {
                    bool first = lineup.ForwardIndex < 0;
                    KeyValuePair<int[], int[]> change = SwapUnit(lineup, true, choice.ForwardIndex);
                    if (!first)
                    {
                        _pendingChanges.Add(change);
                    }
                }

                if (choice.PairIndex != lineup.PairIndex)
                {
                    bool first = lineup.PairIndex < 0;
                    KeyValuePair<int[], int[]> change = SwapUnit(lineup, false, choice.PairIndex);
                    if (!first)
                    {
                        _pendingChanges.Add(change);
                    }
                }
            }
        }

        private void LogStartChanges()
        {
            foreach (KeyValuePair<int[], int[]> change in _pendingChanges)
            {
                _log.Append(new LineChangeEvent(Context(), change.Key, change.Value));
            }

            _pendingChanges.Clear();
        }

        /// <summary>
        /// O-2 safe moment for the team with the puck: if a unit is eligible, the host (coach) may change it. The incoming
        /// skaters take the same nodes; a changed position loses its commitments and net drive.
        /// </summary>
        private void TryChangeOnTheFly(TeamSide team)
        {
            if (_host == null)
            {
                return;
            }

            LineupState lineup = _game.Lineup(team);
            bool forwards = _time - lineup.ForwardSince >= _tuning.Time.ForwardShiftSeconds - TimeEpsilon;
            bool defence = _time - lineup.PairSince >= _tuning.Time.DefenceShiftSeconds - TimeEpsilon;
            if (!forwards && !defence)
            {
                return;
            }

            LineChoice choice = _host.OnTheFly(team, forwards, defence, _time);
            if (choice == null)
            {
                return;
            }

            if (choice.ForwardIndex != lineup.ForwardIndex)
            {
                if (!forwards)
                {
                    throw new InvalidOperationException("The coach changed a forward trio that is not eligible (O-2).");
                }

                ChangeOnIce(team, lineup, true, choice.ForwardIndex);
            }

            if (choice.PairIndex != lineup.PairIndex)
            {
                if (!defence)
                {
                    throw new InvalidOperationException("The coach changed a defence pair that is not eligible (O-2).");
                }

                ChangeOnIce(team, lineup, false, choice.PairIndex);
            }
        }

        private void ChangeOnIce(TeamSide team, LineupState lineup, bool forwards, int index)
        {
            KeyValuePair<int[], int[]> change = SwapUnit(lineup, forwards, index);
            _state.ReplaceUnit(team, lineup.OnIce);
            foreach (Position position in forwards ? Tactics.PositionOrder.Forwards : new[] { Position.LeftDefence, Position.RightDefence })
            {
                _committed[(int)team, (int)position] = false;
                _droveNet[(int)team, (int)position] = false;
            }

            _log.Append(new LineChangeEvent(Context(), change.Key, change.Value));
        }

        /// <summary>Energy and lineup bookkeeping of a unit change; returns (outgoing ids, incoming ids).</summary>
        private KeyValuePair<int[], int[]> SwapUnit(LineupState lineup, bool forwards, int index)
        {
            IReadOnlyList<Skater> outgoing = forwards
                ? (lineup.ForwardIndex >= 0 ? lineup.Team.ForwardLines[lineup.ForwardIndex].Skaters : new Skater[0])
                : (lineup.PairIndex >= 0 ? lineup.Team.DefencePairs[lineup.PairIndex].Skaters : new Skater[0]);
            IReadOnlyList<Skater> incoming = forwards ? lineup.Team.ForwardLines[index].Skaters : lineup.Team.DefencePairs[index].Skaters;
            var outIds = new int[outgoing.Count];
            for (int i = 0; i < outgoing.Count; i++)
            {
                outIds[i] = outgoing[i].Id;
                _game.Energy.Bench(outgoing[i].Id, _time);
            }

            var inIds = new int[incoming.Count];
            for (int i = 0; i < incoming.Count; i++)
            {
                int id = incoming[i].Id;
                inIds[i] = id;
                if (_game.Energy.IsBenched(id, out double since))
                {
                    _game.Energy.Set(id, StateDynamics.BenchRecovered(_game.Energy.Get(id), _time - since, _tuning.Energy.BenchRecoveryRate));
                    _game.Energy.Unbench(id);
                }
            }

            if (forwards)
            {
                lineup.SetForwards(index, _time);
            }
            else
            {
                lineup.SetPair(index, _time);
            }

            return new KeyValuePair<int[], int[]>(outIds, inIds);
        }
    }
}
