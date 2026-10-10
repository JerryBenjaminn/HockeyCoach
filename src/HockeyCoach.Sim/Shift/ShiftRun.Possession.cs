using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>Possession and play mode: gaining the puck, play selection and setup (D-036, D-038), beats and system mode.</summary>
    internal sealed partial class ShiftRun
    {
        /// <summary>D-036: the team gains the puck with <paramref name="holder"/>; choose a play or keep it in system mode.</summary>
        private void GainPossession(TeamSide team, Position holder)
        {
            SetAttacker(team);
            _play = null;
            TrySelectPlay(team, holder);
        }

        private void TrySelectPlay(TeamSide team, Position holder)
        {
            RinkZone zone = _rink.ZoneAtX(TeamView(team, team, holder).X);
            Play play = PlaySelector.Select(Team(team).Plan, zone, holder, out bool needsPass);
            if (play == null)
            {
                _mode = Mode.SystemPossession;
                return;
            }

            GridPoint puck = TeamFrame.ToTeamView(_state.PuckNode, team, _rink);
            Play used = PlayMirror.ShouldMirror(play, puck, _rink) ? PlayMirror.Mirror(play, _rink) : play;
            Setup(team, used);
            BeginPlay(used, needsPass);
        }

        /// <summary>D-038: everyone moves to the play's start nodes and the defence to its system targets; costs setup time.</summary>
        private void Setup(TeamSide team, Play play)
        {
            Spend(_tuning.Time.SetupSeconds);
            MoveSkaters(team, play.StartPositions);
            TeamSide defending = TeamSides.Opponent(team);
            _state.ApplySystem(defending, Team(defending).Plan.System, Unbounded);
            ClearActionHistory();
        }

        private void BeginPlay(Play play, bool needsPass)
        {
            _play = play;
            _beat = 0;
            _mode = needsPass ? Mode.PlayPass : Mode.PlayBeat;
        }

        /// <summary>D-036: a checked pass from the holder to the play's puck carrier starts the play.</summary>
        private void InitialPass()
        {
            TeamSide team = _attacker;
            Position holder = _state.CarrierPosition.Value;
            if (holder == _play.PuckCarrier || Pass(team, holder, _play.PuckCarrier))
            {
                _mode = Mode.PlayBeat;
            }
        }

        private void RunBeat()
        {
            TeamSide team = _attacker;
            Beat beat = _play.Beats[_beat];
            MoveSkaters(team, beat.Moves);

            TeamSide defending = TeamSides.Opponent(team);
            _state.ApplySystem(defending, Team(defending).Plan.System, _tuning.Plays.MaxNodesPerBeat);

            bool continues;
            PlayAction action = beat.Action;
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
            TeamSide team = _attacker;
            Spend("systemStep");
            TeamSide defending = TeamSides.Opponent(team);
            _state.ApplySystem(defending, Team(defending).Plan.System, _tuning.Plays.MaxNodesPerBeat);
            ClearActionHistory();
            TrySelectPlay(team, _state.CarrierPosition.Value);
        }

        /// <summary>A takeaway: log it, remember it for the turnover chance type, and hand possession over.</summary>
        private void Takeaway(TeamSide loserTeam, Position loser, TeamSide takerTeam, Position taker)
        {
            GridPoint node = _state.NodeOf(takerTeam, taker);
            _play = null;
            _log.Append(new TurnoverEvent(Context(), _state.PlayerId(loserTeam, loser), _state.PlayerId(takerTeam, taker), _rink.IdOf(node.X, node.Y)));
            _takeawayTime[(int)takerTeam] = _time;
            _takeawayNode[(int)takerTeam] = node;
            GainPossession(takerTeam, taker);
        }

        private void SetAttacker(TeamSide team)
        {
            _hasAttacker = true;
            _attacker = team;
        }
    }
}
