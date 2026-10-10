using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>The opening faceoff: formations, the draw and its receiver (D-042, D-060, O-3).</summary>
    internal sealed partial class ShiftRun
    {
        private IReadOnlyDictionary<Position, GridPoint> FaceoffStart(TeamSide team, FaceoffSpot spot)
        {
            GridPoint teamSpot = TeamFrame.ToTeamView(spot.Point, team, _rink);
            string teamSpotId = SpotIdAt(teamSpot);
            Play play = teamSpotId == null ? null : PlaySelector.FaceoffPlay(Team(team).Plan, teamSpotId, out bool mirrored);
            if (play == null)
            {
                return FaceoffFormation.Default(teamSpot, _rink);
            }

            Play used = PlayMirror.ShouldMirrorAtSpot(play, teamSpotId) ? PlayMirror.Mirror(play, _rink) : play;
            _faceoffPlays[(int)team] = used;
            return used.StartPositions;
        }

        private void Faceoff()
        {
            if (!Begin("faceoff"))
            {
                return;
            }

            // O-3, O-6: both defences are organized at every faceoff.
            _game.SetOrganization(TeamSide.Home, 1.0);
            _game.SetOrganization(TeamSide.Away, 1.0);
            CheckDefinition check = _tuning.GetCheck("faceoff");
            Skater home = _state.SkaterAt(TeamSide.Home, Position.Center);
            Skater away = _state.SkaterAt(TeamSide.Away, Position.Center);
            double m = check.Modifiers.Get("homeAdvantage")
                + OffSide(new[] { home }, TeamSide.Home, new[] { away }, TeamSide.Away)
                + EnergyModifier(new[] { home.Id }, new[] { away.Id });
            CheckResult result = SidedCheck.Resolve(
                _tuning.CheckFormula,
                check,
                new CheckParticipants().With("centre", home),
                new CheckParticipants().With("centre", away),
                m,
                _random);
            PayCost(home, away);
            TeamSide winner = result.Success ? TeamSide.Home : TeamSide.Away;
            GridPoint spot = _state.PuckNode;
            Spend("faceoff");
            Play faceoffPlay = _faceoffPlays[(int)winner];
            Position receiver = faceoffPlay != null ? faceoffPlay.PuckCarrier : DotSideDefence(winner, spot);
            _state.GivePuckTo(winner, receiver);
            _log.Append(new FaceoffEvent(Context(null, null), home.Id, away.Id, winner, LocationOf(spot)));

            StartPossession(winner, ZoneFor(winner, spot));
            SetAttacker(winner);
            _play = null;
            if (faceoffPlay != null)
            {
                BeginPlay(faceoffPlay, false);
            }
            else
            {
                SelectNext(_tuning.Time.SetupSeconds);
            }
        }

        /// <summary>
        /// D-060 default receiver without a faceoff play: the defenceman on the dot's side in the winner's view (left spot
        /// and the middle lane → LD, right → RD).
        /// </summary>
        private Position DotSideDefence(TeamSide winner, GridPoint spot)
        {
            GridPoint view = TeamFrame.ToTeamView(spot, winner, _rink);
            return _rink.LaneOf(view.Y) == Lane.Right ? Position.RightDefence : Position.LeftDefence;
        }

        private FaceoffLocation LocationOf(GridPoint homeView)
        {
            switch (_rink.ZoneAtX(homeView.X))
            {
                case RinkZone.Defensive: return FaceoffLocation.HomeZone;
                case RinkZone.Offensive: return FaceoffLocation.AwayZone;
                default: return FaceoffLocation.NeutralZone;
            }
        }

        private FaceoffSpot FindSpot(string id)
        {
            foreach (FaceoffSpot spot in _rink.FaceoffSpots)
            {
                if (string.Equals(spot.Id, id, StringComparison.Ordinal))
                {
                    return spot;
                }
            }

            throw new ArgumentException("Unknown faceoff spot " + id + ".");
        }

        private string SpotIdAt(GridPoint point)
        {
            foreach (FaceoffSpot spot in _rink.FaceoffSpots)
            {
                if (spot.Point.Equals(point))
                {
                    return spot.Id;
                }
            }

            return null;
        }
    }
}
