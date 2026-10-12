using System;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// Pure helpers of the contact rules (D-089 A2, data-schema.md, replay.json, Kontaktit) used by
    /// <see cref="SkaterContacts"/>. Positions and velocities are path values (before idle motion).
    /// </summary>
    public static class SkaterAvoidance
    {
        /// <summary>Distances and speeds below this are treated as zero.</summary>
        public const double Coincident = 1e-9;

        /// <summary>Smoothstep 3x² − 2x³ of x clamped to [0, 1].</summary>
        public static double Smooth(double x)
        {
            x = Math.Max(0, Math.Min(1, x));
            return x * x * (3 - (2 * x));
        }

        /// <summary>
        /// Teammate weight: max(time weight, distance weight). Time weight = β × smoothstep(1 − |τ| /
        /// <c>teammateAnticipationSeconds</c>), τ = −(r · u) / |u|² the time to the closest approach (negative after it),
        /// β = smoothstep(|u| / <c>teammateRelativeSpeedMetresPerSecond</c>). Distance weight = 1 at d ≤ m,
        /// smoothstep((2m − d) / m) up to 2m, 0 beyond; m = <c>separationMinMetres</c>.
        /// </summary>
        /// <param name="r">Pᵢ − Pⱼ.</param>
        /// <param name="u">Vᵢ − Vⱼ.</param>
        /// <param name="skaters">The skater settings.</param>
        /// <param name="distanceWeight">The distance weight alone.</param>
        public static double TeammateWeight(RinkPoint r, RinkPoint u, SkaterSettings skaters, out double distanceWeight)
        {
            if (skaters == null)
            {
                throw new ArgumentNullException(nameof(skaters));
            }

            double m = skaters.SeparationMinMetres;
            double d = Length(r);
            distanceWeight = d <= m ? 1 : d >= 2 * m ? 0 : Smooth(((2 * m) - d) / m);
            double uu = (u.X * u.X) + (u.Y * u.Y);
            double time = 0;
            if (uu > Coincident)
            {
                double tau = -((r.X * u.X) + (r.Y * u.Y)) / uu;
                double beta = Smooth(Math.Sqrt(uu) / skaters.TeammateRelativeSpeedMetresPerSecond);
                time = Math.Abs(tau) < skaters.TeammateAnticipationSeconds ? beta * Smooth(1 - (Math.Abs(tau) / skaters.TeammateAnticipationSeconds)) : 0;
            }

            return Math.Max(time, distanceWeight);
        }

        /// <summary>
        /// The fixed sidestep axis of a teammate pair, chosen when its contact starts: across their relative motion
        /// (perp(u)), on the side player i already is, when they move relative to each other at least half of
        /// <c>teammateRelativeSpeedMetresPerSecond</c>; otherwise along r (or <paramref name="fallbackAngle"/> when they coincide).
        /// </summary>
        public static RinkPoint TeammateAxis(RinkPoint r, RinkPoint u, double fallbackAngle, SkaterSettings skaters)
        {
            if (skaters == null)
            {
                throw new ArgumentNullException(nameof(skaters));
            }

            double un = Length(u);
            if (un >= skaters.TeammateRelativeSpeedMetresPerSecond / 2)
            {
                var perp = new RinkPoint(-u.Y / un, u.X / un);
                double side = (r.X * perp.X) + (r.Y * perp.Y);
                return side >= 0 ? perp : new RinkPoint(-perp.X, -perp.Y);
            }

            double d = Length(r);
            return d > Coincident ? new RinkPoint(r.X / d, r.Y / d) : new RinkPoint(Math.Cos(fallbackAngle), Math.Sin(fallbackAngle));
        }

        /// <summary>
        /// The teammate push for player i (j gets the opposite): w × max(0, m − r · a) × a / 2 along the pair's fixed axis
        /// a, so at full weight their separation along the axis is at least m: they pass side by side or stand apart, and
        /// neither flips side nor circles the other.
        /// </summary>
        public static RinkPoint TeammatePush(RinkPoint r, RinkPoint axis, double weight, SkaterSettings skaters)
        {
            if (skaters == null)
            {
                throw new ArgumentNullException(nameof(skaters));
            }

            double q = (r.X * axis.X) + (r.Y * axis.Y);
            double push = weight * Math.Max(0, skaters.SeparationMinMetres - q) / 2;
            return new RinkPoint(axis.X * push, axis.Y * push);
        }

        /// <summary>
        /// Opponent weight: 1 at d ≤ <c>opponentPlaceMetres</c>, smoothstep((contact − d) / (contact − place)) up to
        /// <c>opponentContactMetres</c>, 0 beyond.
        /// </summary>
        public static double OpponentWeight(double d, SkaterSettings skaters)
        {
            if (skaters == null)
            {
                throw new ArgumentNullException(nameof(skaters));
            }

            double contact = skaters.OpponentContactMetres;
            double place = skaters.OpponentPlaceMetres;
            return d <= place ? 1 : d >= contact ? 0 : Smooth((contact - d) / (contact - place));
        }

        /// <summary>
        /// The placement axis at the pair's midpoint <paramref name="mid"/>, as a unit vector from the away player towards
        /// the home player: unit(towards the home net − towards the away net + the rink axis from the away net to the home
        /// net). It always has a −x part, so it never flips: each team's defender is on his own goal side.
        /// </summary>
        public static RinkPoint Axis(RinkMapping mapping, RinkPoint mid)
        {
            if (mapping == null)
            {
                throw new ArgumentNullException(nameof(mapping));
            }

            RinkPoint homeNet = mapping.NetOf(ReplayTeam.Home);
            RinkPoint awayNet = mapping.NetOf(ReplayTeam.Away);
            RinkPoint toHome = Unit(homeNet.X - mid.X, homeNet.Y - mid.Y);
            RinkPoint toAway = Unit(awayNet.X - mid.X, awayNet.Y - mid.Y);
            RinkPoint rink = Unit(homeNet.X - awayNet.X, homeNet.Y - awayNet.Y);
            return Unit(toHome.X - toAway.X + rink.X, toHome.Y - toAway.Y + rink.Y);
        }

        /// <summary>
        /// Turns unit vector <paramref name="from"/> towards unit vector <paramref name="to"/> by at most
        /// <paramref name="maxRadians"/> (the shorter way; exactly opposite turns counter-clockwise).
        /// </summary>
        public static RinkPoint Turn(RinkPoint from, RinkPoint to, double maxRadians)
        {
            double angle = Math.Atan2((from.X * to.Y) - (from.Y * to.X), (from.X * to.X) + (from.Y * to.Y));
            if (Math.Abs(angle) <= maxRadians)
            {
                return to;
            }

            double step = Math.Sign(angle) * maxRadians;
            double c = Math.Cos(step);
            double s = Math.Sin(step);
            return new RinkPoint((from.X * c) - (from.Y * s), (from.X * s) + (from.Y * c));
        }

        /// <summary>Unit vector, zero for a zero vector.</summary>
        public static RinkPoint Unit(double x, double y)
        {
            double n = Math.Sqrt((x * x) + (y * y));
            return n > 0 ? new RinkPoint(x / n, y / n) : new RinkPoint(0, 0);
        }

        /// <summary>Length of a vector.</summary>
        public static double Length(RinkPoint v)
        {
            return Math.Sqrt((v.X * v.X) + (v.Y * v.Y));
        }
    }
}
