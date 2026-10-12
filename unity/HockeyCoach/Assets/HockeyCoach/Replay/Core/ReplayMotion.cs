using System;
using System.Collections.Generic;

namespace HockeyCoach.Replay.Core
{
    /// <summary>The drawn positions at one presentation time.</summary>
    public sealed class MotionSample
    {
        /// <summary>Creates the sample.</summary>
        public MotionSample(IReadOnlyList<PlayerSample> players, IReadOnlyList<PlayerSample> leaving, RinkPoint puck, int? puckCarrierId)
        {
            Players = players;
            Leaving = leaving;
            Puck = puck;
            PuckCarrierId = puckCarrierId;
        }

        /// <summary>The on-ice players of the segment (skaters and goalies) in id order.</summary>
        public IReadOnlyList<PlayerSample> Players { get; }

        /// <summary>Players still skating off to the bench gate, in id order.</summary>
        public IReadOnlyList<PlayerSample> Leaving { get; }

        /// <summary>Puck position.</summary>
        public RinkPoint Puck { get; }

        /// <summary>Puck carrier, null while the puck is loose or moving.</summary>
        public int? PuckCarrierId { get; }
    }

    /// <summary>
    /// The motion layer of the replay (D-088): skaters (<see cref="SkaterMotion"/>), the puck and the goalies, sampled
    /// statelessly at a presentation time. Built once per <see cref="PresentationTimeline"/>.
    /// </summary>
    public sealed class ReplayMotion
    {
        private ReplayMotion(PresentationTimeline presentation, SkaterMotion skaters, PuckMotion puck)
        {
            Presentation = presentation;
            Skaters = skaters;
            Puck = puck;
        }

        /// <summary>The presentation timeline.</summary>
        public PresentationTimeline Presentation { get; }

        /// <summary>The skater motion.</summary>
        public SkaterMotion Skaters { get; }

        /// <summary>The puck motion.</summary>
        public PuckMotion Puck { get; }

        /// <summary>Builds the motion.</summary>
        public static ReplayMotion Build(PresentationTimeline presentation)
        {
            if (presentation == null)
            {
                throw new ArgumentNullException(nameof(presentation));
            }

            SkaterMotion skaters = SkaterMotion.Build(presentation);
            return new ReplayMotion(presentation, skaters, PuckMotion.Build(presentation, skaters));
        }

        /// <summary>The drawn state of segment <paramref name="segmentIndex"/> at presentation time <paramref name="t"/>.</summary>
        public MotionSample Sample(int segmentIndex, double t)
        {
            TimelineSegment segment = Presentation.Timeline.Segments[segmentIndex];
            SkaterPose[] poses = Skaters.Sample(segmentIndex, t);
            int? carrier;
            RinkPoint puck = Puck.PositionAt(segmentIndex, t, poses, out carrier);
            return Compose(segment, poses, puck, carrier);
        }

        /// <summary>
        /// A goalie's position (data-schema.md, replay.json, Maalivahdit): <c>depthMetres</c> in front of his goal line,
        /// laterally the net centre + clamp(<c>lateralTrackFactor</c> × (puck y − net y), ±<c>lateralMaxMetres</c>).
        /// </summary>
        public static RinkPoint GoaliePosition(RinkMapping mapping, GoalieSettings goalies, ReplayTeam team, double puckY)
        {
            if (mapping == null)
            {
                throw new ArgumentNullException(nameof(mapping));
            }

            if (goalies == null)
            {
                throw new ArgumentNullException(nameof(goalies));
            }

            RinkPoint net = mapping.NetOf(team);
            double x = team == ReplayTeam.Home ? mapping.HomeGoalLineX + goalies.DepthMetres : mapping.AwayGoalLineX - goalies.DepthMetres;
            double lateral = Math.Max(-goalies.LateralMaxMetres, Math.Min(goalies.LateralMaxMetres, goalies.LateralTrackFactor * (puckY - net.Y)));
            return new RinkPoint(x, net.Y + lateral);
        }

        private MotionSample Compose(TimelineSegment segment, SkaterPose[] poses, RinkPoint puck, int? carrier)
        {
            var skaterAt = new Dictionary<int, RinkPoint>();
            var leaving = new List<PlayerSample>();
            foreach (SkaterPose pose in poses)
            {
                if (pose.IsLeaving)
                {
                    leaving.Add(new PlayerSample(pose.Id, pose.Team, false, pose.Position));
                }
                else
                {
                    skaterAt[pose.Id] = pose.Position;
                }
            }

            var players = new List<PlayerSample>(segment.Players.Count);
            foreach (SegmentPlayer p in segment.Players)
            {
                RinkPoint position;
                if (p.IsGoalie)
                {
                    position = GoaliePosition(Presentation.Timeline.Mapping, Presentation.Config.Goalies, p.Team, puck.Y);
                }
                else if (!skaterAt.TryGetValue(p.Id, out position))
                {
                    position = p.To;
                }

                players.Add(new PlayerSample(p.Id, p.Team, p.IsGoalie, position));
            }

            return new MotionSample(players, leaving, puck, carrier);
        }
    }
}
