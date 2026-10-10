using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// Possession and play mode: gaining the puck and the transition instruction (O-7), play selection and setup (D-036,
    /// D-038, O-6), beats and system mode, turnovers and the organization drop (O-6), and the possession facts the
    /// chance types read (O-12).
    /// </summary>
    internal sealed partial class ShiftRun
    {
        private readonly LastPuckAction[] _lastAction = new LastPuckAction[2];
        private readonly double[] _gainTime = { double.NaN, double.NaN };
        private readonly RinkZone[] _gainZone = new RinkZone[2];
        private bool _hasPossession;
        private TeamSide _possessionTeam;
        private RinkZone _possessionStartZone;
        private bool _enteredZone;
        private bool _hasEntryNumbers;
        private EntryNumbers _entryNumbers;

        private enum GainKind
        {
            Interception,
            Takeaway,
            LooseWin,
        }

        /// <summary>A new possession for <paramref name="team"/> starting in <paramref name="zone"/> (its view) (O-12).</summary>
        private void StartPossession(TeamSide team, RinkZone zone)
        {
            _hasPossession = true;
            _possessionTeam = team;
            _possessionStartZone = zone;
            _enteredZone = zone == RinkZone.Offensive;
            _hasEntryNumbers = false;
        }

        /// <summary>A controlled entry that kept the puck: pressure gain (O-9) and the N vs M of the possession (goal notes).</summary>
        private void EntryKept(TeamSide team, EntryNumbers numbers)
        {
            GainPressure(team, _tuning.Pressure.GainOnZoneEntry);
            if (_hasPossession && _possessionTeam == team)
            {
                _hasEntryNumbers = true;
                _entryNumbers = numbers;
            }
        }

        /// <summary>O-12: the puck reached the offensive zone during the current possession.</summary>
        private void TrackEntry()
        {
            if (_hasPossession && _state.HasCarrier && _state.CarrierTeam == _possessionTeam
                && ZoneFor(_possessionTeam, _state.PuckNode) == RinkZone.Offensive)
            {
                _enteredZone = true;
            }
        }

        /// <summary>
        /// The team gains the puck other than from a faceoff (O-7): the transition instruction decides rush, regroup or a
        /// play choice. <paramref name="fromOpponent"/> is a gain from the opponent (interception, takeaway, or a loose puck
        /// the opponent had last).
        /// </summary>
        private void GainPossession(TeamSide team, Position holder, GainKind kind, bool fromOpponent)
        {
            NotePossession(team, holder, fromOpponent);
            SetAttacker(team);
            _play = null;
            _prepaid = false;
            RinkZone zone = _rink.ZoneAtX(TeamView(team, team, holder).X);
            if (Team(team).Plan.Transition == TransitionInstruction.Rush)
            {
                _rushActions = 0;
                _mode = Mode.Rush;
            }
            else if (zone != RinkZone.Offensive)
            {
                _mode = Mode.Regroup;
            }
            else
            {
                SelectNext(_tuning.Time.SetupSeconds);
            }
        }

        private void NotePossession(TeamSide team, Position holder, bool fromOpponent)
        {
            RinkZone zone = _rink.ZoneAtX(TeamView(team, team, holder).X);
            if (fromOpponent || !_hasPossession || _possessionTeam != team)
            {
                StartPossession(team, zone);
            }

            if (fromOpponent)
            {
                _gainTime[(int)team] = _time;
                _gainZone[(int)team] = zone;
            }
        }

        /// <summary>The next step is a D-036 play choice whose setup lasts <paramref name="setupSeconds"/> for the defence (O-6).</summary>
        private void SelectNext(double setupSeconds)
        {
            _setupSteps = SetupSteps(setupSeconds);
            _mode = Mode.SelectPlay;
        }

        /// <summary>The D-036 play choice; also an on-the-fly change moment when the puck is outside the own zone (O-2).</summary>
        private void SelectPlayStep()
        {
            TeamSide team = _attacker;
            Position holder = _state.CarrierPosition.Value;
            if (TeamView(team, team, holder).X > DefensiveZoneMaxX)
            {
                TryChangeOnTheFly(team);
            }

            RinkZone zone = _rink.ZoneAtX(TeamView(team, team, holder).X);
            Play play = PlaySelector.Select(Team(team).Plan, zone, holder, out bool needsPass);
            if (play == null)
            {
                _mode = Mode.SystemPossession;
                return;
            }

            GridPoint puck = TeamFrame.ToTeamView(_state.PuckNode, team, _rink);
            Play used = PlayMirror.ShouldMirror(play, puck, _rink) ? PlayMirror.Mirror(play, _rink) : play;
            double duration = _tuning.Time.SetupSeconds + (needsPass ? Seconds("pass") : 0.0) + ActionDuration(used.Beats[0].Action);
            if (!Begin(duration))
            {
                return;
            }

            Setup(team, used, _setupSteps);
            BeginPlay(used, needsPass);
            _prepaid = true;
        }

        private int DefensiveZoneMaxX
        {
            get
            {
                foreach (ZoneRange zone in _rink.Zones)
                {
                    if (zone.Zone == RinkZone.Defensive)
                    {
                        return zone.XMax;
                    }
                }

                return -1;
            }
        }

        /// <summary>O-1: the time of a beat's action as one sequence (shot with rebound, dump with its first battle).</summary>
        private double ActionDuration(PlayAction action)
        {
            switch (action.Type)
            {
                case PlayActionType.Skate: return Seconds("skate");
                case PlayActionType.Pass: return Seconds("pass");
                case PlayActionType.DriveNet: return Seconds("driveNet");
                case PlayActionType.Shoot: return Seconds("shoot") + Seconds("rebound");
                default: return Seconds("dumpIn") + Seconds("loosePuck");
            }
        }

        /// <summary>
        /// D-038, O-6: the attackers move to the play's start nodes; the defence moves at most <paramref name="defenceSteps"/>
        /// toward its system targets. Costs setup time; the team's commitments end (O-10).
        /// </summary>
        private void Setup(TeamSide team, Play play, int defenceSteps)
        {
            Spend(_tuning.Time.SetupSeconds);
            ClearCommitments(team);
            MoveSkaters(team, play.StartPositions);
            ApplyDefence(TeamSides.Opponent(team), defenceSteps);
            ClearActionHistory();
            _setupSteps = SetupSteps(_tuning.Time.SetupSeconds);
        }

        /// <summary>A play starts: counted for familiarity (O-11, D-066), including faceoff and aborted plays.</summary>
        private void BeginPlay(Play play, bool needsPass)
        {
            _play = play;
            _beat = 0;
            double uses = _game.Familiarity.Start(_attacker, play.Id);
            _familiarityPenalty = StateDynamics.FamiliarityPenalty(
                uses, _tuning.Familiarity.FreeUses, _tuning.Familiarity.PenaltyPerRepeat, _tuning.Familiarity.MaxPenalty);
            _mode = needsPass ? Mode.PlayPass : Mode.PlayBeat;
        }

        /// <summary>D-036: a checked pass from the holder to the play's puck carrier starts the play (inside the setup sequence).</summary>
        private void InitialPass()
        {
            TeamSide team = _attacker;
            Position holder = _state.CarrierPosition.Value;
            if (!_prepaid && !Begin("pass"))
            {
                return;
            }

            if (holder == _play.PuckCarrier || Pass(team, holder, _play.PuckCarrier))
            {
                _mode = Mode.PlayBeat;
            }
        }

        private void RunBeat()
        {
            TeamSide team = _attacker;
            Beat beat = _play.Beats[_beat];
            PlayAction action = beat.Action;
            if (_prepaid)
            {
                _prepaid = false;
            }
            else if (!Begin(ActionDuration(action)))
            {
                return;
            }

            MoveSkaters(team, beat.Moves);
            TeamSide defending = TeamSides.Opponent(team);
            ApplyDefence(defending, _tuning.Plays.MaxNodesPerBeat);

            bool continues;
            switch (action.Type)
            {
                case PlayActionType.Skate:
                    continues = Skate(team, action.Actor, action.Target.Value);
                    break;
                case PlayActionType.Pass:
                    continues = Pass(team, action.Actor, action.Receiver.Value);
                    break;
                case PlayActionType.DriveNet:
                    DriveNet(team, action.Actor);
                    continues = true;
                    break;
                case PlayActionType.Shoot:
                    Shoot(team, action.Actor);
                    continues = false;
                    break;
                default:
                    Dump(team, action.Actor, action.Target.Value);
                    continues = false;
                    break;
            }

            if (!continues || _mode != Mode.PlayBeat)
            {
                return;
            }

            _beat++;
            if (_beat >= _play.Beats.Count)
            {
                _play = null;
                _mode = Mode.SystemPossession;
            }
        }

        /// <summary>System mode with the puck: one system step (the defence moves), then a new play choice (Q-033 default).</summary>
        private void SystemPossessionStep()
        {
            if (!Begin("systemStep"))
            {
                return;
            }

            TeamSide team = _attacker;
            Spend("systemStep");
            ApplyDefence(TeamSides.Opponent(team), _tuning.Plays.MaxNodesPerBeat);
            ClearActionHistory();
            SelectNext(_tuning.Time.SetupSeconds);
        }

        /// <summary>A takeaway or interception: log it, drop the loser's organization and hand possession over.</summary>
        private void Takeaway(TeamSide loserTeam, Position loser, TeamSide takerTeam, Position taker, GainKind kind)
        {
            GridPoint node = _state.NodeOf(takerTeam, taker);
            _play = null;
            _log.Append(new TurnoverEvent(Context(), _state.PlayerId(loserTeam, loser), _state.PlayerId(takerTeam, taker), _rink.IdOf(node.X, node.Y)));
            DropOrganization(loserTeam, node);
            GainPossession(takerTeam, taker, kind, true);
        }

        /// <summary>
        /// O-6 turnover: the losing team's organization drops by the zone where the opponent got the puck (losing team's
        /// view), halved after its own shot or dump, and more per committed player.
        /// </summary>
        private void DropOrganization(TeamSide loser, GridPoint gainNode)
        {
            double drop = _tuning.Organization.DropIn(ZoneFor(loser, gainNode));
            double factor = _lastAction[(int)loser] == LastPuckAction.ShotOrDump ? _tuning.Organization.DropFactorOnShotOrDump : 1.0;
            _game.SetOrganization(
                loser,
                StateDynamics.OrganizationAfterTurnover(_game.Organization(loser), drop, factor, _tuning.Organization.DropPerCommittedPlayer, Committed(loser)));
        }

        private void SetAttacker(TeamSide team)
        {
            _hasAttacker = true;
            _attacker = team;
        }
    }
}
