using System;
using System.Collections.Generic;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// One straight leg of a puck flight from <see cref="From"/> to a fixed point or, when <see cref="TargetId"/> is set,
    /// homing on that player's carried puck position (data-schema.md, replay.json, Kiekko: "hakeutuu vastaanottajan
    /// liikkuvaan sijaintiin"): at time t the puck is at lerp(From, target(t), (t − Start) / (End − Start)), so it reaches
    /// the player exactly at <see cref="End"/>.
    /// </summary>
    public sealed class PuckLeg
    {
        /// <summary>Creates a leg.</summary>
        public PuckLeg(RinkPoint from, RinkPoint to, int? targetId, double start, double end, double speed)
        {
            From = from;
            To = to;
            TargetId = targetId;
            Start = start;
            End = end;
            Speed = speed;
        }

        /// <summary>Start point.</summary>
        public RinkPoint From { get; }

        /// <summary>End point (for a homing leg: the target's position at <see cref="End"/>).</summary>
        public RinkPoint To { get; }

        /// <summary>The player the leg homes on, null for a fixed end point.</summary>
        public int? TargetId { get; }

        /// <summary>Presentation time the leg starts.</summary>
        public double Start { get; }

        /// <summary>Presentation time the leg ends.</summary>
        public double End { get; }

        /// <summary>Configured speed of the leg (0 for a hold on the faceoff spot).</summary>
        public double Speed { get; }
    }

    /// <summary>
    /// The puck of one segment (data-schema.md, replay.json, Kiekko): before <see cref="WindowStart"/> the puck keeps the
    /// previous segment's state; from it the flight <see cref="Legs"/> run; after them the puck is carried by
    /// <see cref="EndCarrierId"/> or lies at <see cref="EndPoint"/>.
    /// </summary>
    public sealed class PuckPlan
    {
        /// <summary>Creates a plan.</summary>
        public PuckPlan(int segmentIndex, double windowStart, RinkPoint start, IReadOnlyList<PuckLeg> legs, int? endCarrierId, RinkPoint endPoint, bool inNet)
        {
            SegmentIndex = segmentIndex;
            WindowStart = windowStart;
            Start = start;
            Legs = legs;
            EndCarrierId = endCarrierId;
            EndPoint = endPoint;
            InNet = inNet;
        }

        /// <summary>Segment index.</summary>
        public int SegmentIndex { get; }

        /// <summary>Presentation time the plan takes over (the start of the action window).</summary>
        public double WindowStart { get; }

        /// <summary>Puck position at <see cref="WindowStart"/> before the flight (the previous state).</summary>
        public RinkPoint Start { get; }

        /// <summary>The flight, empty when the puck does not fly.</summary>
        public IReadOnlyList<PuckLeg> Legs { get; }

        /// <summary>End of the flight (= <see cref="WindowStart"/> without one).</summary>
        public double FlightEnd => Legs.Count > 0 ? Legs[Legs.Count - 1].End : WindowStart;

        /// <summary>Carrier after the flight, null for a loose puck.</summary>
        public int? EndCarrierId { get; }

        /// <summary>Where a loose puck lies after the flight.</summary>
        public RinkPoint EndPoint { get; }

        /// <summary>True while the puck is in the net after a goal (Q-079): it stays there until the next faceoff's window.</summary>
        public bool InNet { get; }
    }

    /// <summary>
    /// Puck motion of the presentation layer (D-088, Q-079): one <see cref="PuckPlan"/> per segment, built once, and a
    /// stateless position at any presentation time. Path ends are the players' drawn positions (passes and loose pucks
    /// home on the new carrier); shots go through the goalie or the blocker, to the miss point or into the net.
    /// </summary>
    public sealed class PuckMotion
    {
        /// <summary>Scan step of the arrival search (presentation seconds).</summary>
        private const double AimScanSeconds = 0.05;

        /// <summary>Longest flight the arrival search looks at (presentation seconds).</summary>
        private const double AimHorizonSeconds = 10;

        /// <summary>Bisection steps of the arrival search.</summary>
        private const int AimBisections = 30;
        private readonly SkaterMotion _skaters;
        private readonly PresentationTimeline _p;
        private readonly PuckPlan[] _plans;
        private readonly Dictionary<int, ReplayTeam> _goalies = new Dictionary<int, ReplayTeam>();

        private PuckMotion(PresentationTimeline presentation, SkaterMotion skaters)
        {
            _p = presentation;
            _skaters = skaters;
            _plans = new PuckPlan[presentation.Segments.Count];
            foreach (TimelineSegment s in presentation.Timeline.Segments)
            {
                foreach (SegmentPlayer p in s.Players)
                {
                    if (p.IsGoalie)
                    {
                        _goalies[p.Id] = p.Team;
                    }
                }
            }
        }

        /// <summary>The plans in segment order.</summary>
        public IReadOnlyList<PuckPlan> Plans => _plans;

        /// <summary>Builds the plans.</summary>
        public static PuckMotion Build(PresentationTimeline presentation, SkaterMotion skaters)
        {
            if (presentation == null)
            {
                throw new ArgumentNullException(nameof(presentation));
            }

            if (skaters == null)
            {
                throw new ArgumentNullException(nameof(skaters));
            }

            var motion = new PuckMotion(presentation, skaters);
            for (int i = 0; i < motion._plans.Length; i++)
            {
                motion._plans[i] = motion.Plan(i);
            }

            return motion;
        }

        /// <summary>
        /// Puck position and carrier in segment <paramref name="segmentIndex"/> at <paramref name="t"/>, given the drawn
        /// skaters of the same sample (<paramref name="poses"/>; carriers not in it are sampled on their own).
        /// </summary>
        public RinkPoint PositionAt(int segmentIndex, double t, IReadOnlyList<SkaterPose> poses, out int? carrier)
        {
            PuckPlan plan = _plans[segmentIndex];
            if (t < plan.WindowStart)
            {
                if (segmentIndex == 0)
                {
                    carrier = null;
                    return plan.Start;
                }

                plan = _plans[segmentIndex - 1];
            }

            return After(plan, t, poses, out carrier);
        }

        /// <summary>The carried puck of <paramref name="id"/>: his drawn position + <c>carriedLeadMetres</c> in his direction of motion (towards the opponent goal when slower than <c>carriedHeadingMinSpeed</c>).</summary>
        public RinkPoint Carried(int id, double t, IReadOnlyList<SkaterPose> poses)
        {
            PuckSettings puck = _p.Config.Puck;
            ReplayTeam team;
            if (_goalies.TryGetValue(id, out team))
            {
                RinkPoint g = ReplayMotion.GoaliePosition(_p.Timeline.Mapping, _p.Config.Goalies, team, _p.Timeline.Mapping.NetOf(team).Y);
                return new RinkPoint(g.X + (Attack(team) * puck.CarriedLeadMetres), g.Y);
            }

            SkaterPose? pose = Find(poses, id);
            if (!pose.HasValue)
            {
                pose = Find(_skaters.Sample(_p.SegmentIndexAt(t), t), id);
            }

            if (!pose.HasValue)
            {
                return _skaters.Tracks.ContainsKey(id) ? _skaters.Tracks[id].StateAt(t).Position : new RinkPoint(0, 0);
            }

            SkaterPose s = pose.Value;
            RinkPoint dir = s.Path.Speed >= puck.CarriedHeadingMinSpeed && (s.Path.Direction.X != 0 || s.Path.Direction.Y != 0)
                ? s.Path.Direction
                : new RinkPoint(Attack(s.Team), 0);
            return new RinkPoint(s.Position.X + (dir.X * puck.CarriedLeadMetres), s.Position.Y + (dir.Y * puck.CarriedLeadMetres));
        }

        private static double Attack(ReplayTeam team)
        {
            return team == ReplayTeam.Home ? 1 : -1;
        }

        private static SkaterPose? Find(IReadOnlyList<SkaterPose> poses, int id)
        {
            if (poses != null)
            {
                foreach (SkaterPose p in poses)
                {
                    if (p.Id == id)
                    {
                        return p;
                    }
                }
            }

            return null;
        }

        private RinkPoint After(PuckPlan plan, double t, IReadOnlyList<SkaterPose> poses, out int? carrier)
        {
            if (t < plan.WindowStart)
            {
                carrier = null;
                return plan.Start;
            }

            foreach (PuckLeg leg in plan.Legs)
            {
                if (t < leg.End)
                {
                    carrier = null;
                    if (t <= leg.Start)
                    {
                        return leg.From;
                    }

                    RinkPoint to = leg.TargetId.HasValue ? Carried(leg.TargetId.Value, t, poses) : leg.To;
                    return RinkPoint.Lerp(leg.From, to, (t - leg.Start) / (leg.End - leg.Start));
                }
            }

            carrier = plan.EndCarrierId;
            return plan.EndCarrierId.HasValue ? Carried(plan.EndCarrierId.Value, t, poses) : plan.EndPoint;
        }

        private PuckPlan Plan(int i)
        {
            TimelineSegment s = _p.Timeline.Segments[i];
            SegmentTiming tm = _p.Segments[i];
            ReplayEvent e = s.Event;
            PuckSettings puck = _p.Config.Puck;
            RinkMapping map = _p.Timeline.Mapping;
            double ws = tm.WindowStart;
            PuckPlan prev = i > 0 ? _plans[i - 1] : null;
            int? holder = e.Placement.PuckCarrierId;
            RinkPoint node = map.NodePosition(e.Placement.PuckNodeId);
            RinkPoint start;
            int? before;
            if (prev == null)
            {
                start = s.FaceoffSpot != null ? s.FaceoffSpot.Position : node;
                before = null;
            }
            else
            {
                start = After(prev, ws, null, out before);
            }

            var legs = new List<PuckLeg>();
            if (prev != null && prev.InNet && e.Kind != ReplayEventType.Faceoff)
            {
                return new PuckPlan(i, ws, start, legs, null, prev.EndPoint, true);
            }

            switch (e.Kind)
            {
                case ReplayEventType.Faceoff:
                {
                    RinkPoint spot = s.FaceoffSpot != null ? s.FaceoffSpot.Position : node;
                    double drawn = ws + puck.FaceoffDrawDelaySeconds;
                    legs.Add(new PuckLeg(spot, spot, null, ws, drawn, 0));
                    Toward(legs, spot, drawn, holder, node, puck.FaceoffDrawSpeed);
                    return End(i, ws, spot, legs, holder, node);
                }

                case ReplayEventType.Pass:
                {
                    RinkPoint aim = AimPoint(holder, node, start, ws, puck.PassSpeed);
                    double speed = RinkPoint.Distance(start, aim) >= puck.LongPassMinMetres ? puck.LongPassSpeed : puck.PassSpeed;
                    Toward(legs, start, ws, holder, node, speed);
                    return End(i, ws, start, legs, holder, node);
                }

                case ReplayEventType.Shot:
                    return Shot(i, s, ws, start, holder, node);
                case ReplayEventType.DumpIn:
                    Toward(legs, start, ws, holder, node, puck.DumpInSpeed);
                    return End(i, ws, start, legs, holder, node);
                case ReplayEventType.Turnover:
                case ReplayEventType.PuckBattle:
                    Toward(legs, start, ws, holder, node, puck.LoosePuckSpeed);
                    return End(i, ws, start, legs, holder, node);
                case ReplayEventType.ControlledZoneEntry:
                    if (e.Method == "Carry" && holder == before)
                    {
                        return new PuckPlan(i, ws, start, legs, holder, start, false);
                    }

                    break;
            }

            if (holder != before && holder.HasValue)
            {
                Toward(legs, start, ws, holder, node, puck.LoosePuckSpeed);
                return End(i, ws, start, legs, holder, node);
            }

            return new PuckPlan(i, ws, start, legs, holder, start, false);
        }

        private PuckPlan Shot(int i, TimelineSegment s, double ws, RinkPoint start, int? holder, RinkPoint node)
        {
            ReplayEvent e = s.Event;
            PuckSettings puck = _p.Config.Puck;
            RinkMapping map = _p.Timeline.Mapping;
            RosterEntry shooterEntry = _p.Timeline.FindPlayer(e.ShooterId.Value);
            ReplayTeam shooter = shooterEntry != null ? ReplayTeams.Parse(shooterEntry.Team) : ReplayTeam.Home;
            ReplayTeam target = ReplayTeams.Opponent(shooter);
            var legs = new List<PuckLeg>();
            if (e.Outcome == "Goal")
            {
                RinkPoint net = PuckGeometry.NetInside(map, target, _p.Config.Goal.NetInsideMetres);
                legs.Add(new PuckLeg(start, net, null, ws, ws + (RinkPoint.Distance(start, net) / puck.ShotSpeed), puck.ShotSpeed));
                return new PuckPlan(i, ws, start, legs, null, net, true);
            }

            RinkPoint via;
            double second;
            if (e.Outcome == "Missed")
            {
                int shooterNode = s.Puck.StartNodeId;
                foreach (SegmentPlayer p in s.Players)
                {
                    if (p.Id == e.ShooterId.Value)
                    {
                        shooterNode = p.NodeId;
                    }
                }

                via = PuckGeometry.MissPoint(map, target, e.Placement.PuckNodeId, shooterNode, puck.MissWideMetres);
                second = puck.ReboundSpeed;
            }
            else if (e.StoppedById.HasValue && _goalies.ContainsKey(e.StoppedById.Value))
            {
                // The goalie tracks the puck laterally; with the puck on him (factor < 1) he stands on the net centre line.
                via = ReplayMotion.GoaliePosition(map, _p.Config.Goalies, target, map.NetOf(target).Y);
                second = puck.ReboundSpeed;
            }
            else if (e.StoppedById.HasValue)
            {
                int blocker = e.StoppedById.Value;
                RinkPoint aim = Aim(blocker, start, ws, puck.ShotSpeed, false);
                double hit = ws + (RinkPoint.Distance(start, aim) / puck.ShotSpeed);
                RinkPoint at = Drawn(blocker, hit);
                legs.Add(new PuckLeg(start, at, null, ws, hit, puck.ShotSpeed));
                Toward(legs, at, hit, holder, node, puck.BlockDeflectSpeed);
                return End(i, ws, start, legs, holder, node);
            }
            else
            {
                Toward(legs, start, ws, holder, node, puck.ShotSpeed);
                return End(i, ws, start, legs, holder, node);
            }

            double reach = ws + (RinkPoint.Distance(start, via) / puck.ShotSpeed);
            legs.Add(new PuckLeg(start, via, null, ws, reach, puck.ShotSpeed));
            Toward(legs, via, reach, holder, node, second);
            return End(i, ws, start, legs, holder, node);
        }

        private static PuckPlan End(int i, double ws, RinkPoint start, List<PuckLeg> legs, int? holder, RinkPoint node)
        {
            return new PuckPlan(i, ws, start, legs, holder, node, false);
        }

        /// <summary>Adds a leg from <paramref name="from"/> at <paramref name="t0"/> to the new holder (homing) or the puck node.</summary>
        private void Toward(List<PuckLeg> legs, RinkPoint from, double t0, int? holder, RinkPoint node, double speed)
        {
            if (holder.HasValue)
            {
                RinkPoint aim = Aim(holder.Value, from, t0, speed, true);
                legs.Add(new PuckLeg(from, aim, holder, t0, t0 + (RinkPoint.Distance(from, aim) / speed), speed));
            }
            else
            {
                legs.Add(new PuckLeg(from, node, null, t0, t0 + (RinkPoint.Distance(from, node) / speed), speed));
            }
        }

        private RinkPoint AimPoint(int? holder, RinkPoint node, RinkPoint from, double t0, double speed)
        {
            return holder.HasValue ? Aim(holder.Value, from, t0, speed, true) : node;
        }

        /// <summary>
        /// The target's position at the puck's arrival: the first root of h(T) = |from − position(t0 + T)| / speed − T,
        /// found by scanning in <see cref="AimScanSeconds"/> steps up to <see cref="AimHorizonSeconds"/> and bisection.
        /// When the target outruns the puck (no root), the point at the horizon's closest approach is used.
        /// </summary>
        private RinkPoint Aim(int id, RinkPoint from, double t0, double speed, bool carried)
        {
            double lo = 0;
            double hLo = Gap(id, from, t0, 0, speed, carried);
            if (!(hLo > 0))
            {
                return Target(id, t0, carried);
            }

            double best = 0;
            double bestH = hLo;
            for (double hi = AimScanSeconds; hi <= AimHorizonSeconds + 1e-9; hi += AimScanSeconds)
            {
                double hHi = Gap(id, from, t0, hi, speed, carried);
                if (hHi <= 0)
                {
                    for (int k = 0; k < AimBisections; k++)
                    {
                        double mid = (lo + hi) / 2;
                        if (Gap(id, from, t0, mid, speed, carried) > 0)
                        {
                            lo = mid;
                        }
                        else
                        {
                            hi = mid;
                        }
                    }

                    return Target(id, t0 + hi, carried);
                }

                if (hHi < bestH)
                {
                    bestH = hHi;
                    best = hi;
                }

                lo = hi;
            }

            return Target(id, t0 + best, carried);
        }

        private double Gap(int id, RinkPoint from, double t0, double flight, double speed, bool carried)
        {
            return (RinkPoint.Distance(from, Target(id, t0 + flight, carried)) / speed) - flight;
        }

        private RinkPoint Target(int id, double t, bool carried)
        {
            return carried ? Carried(id, t, null) : Drawn(id, t);
        }

        private RinkPoint Drawn(int id, double t)
        {
            SkaterPose? pose = Find(_skaters.Sample(_p.SegmentIndexAt(t), t), id);
            if (pose.HasValue)
            {
                return pose.Value.Position;
            }

            return _skaters.Tracks.ContainsKey(id) ? _skaters.Tracks[id].StateAt(t).Position : new RinkPoint(0, 0);
        }
    }
}
