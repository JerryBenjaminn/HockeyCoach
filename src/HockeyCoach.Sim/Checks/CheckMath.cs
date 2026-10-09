using System;

namespace HockeyCoach.Sim.Checks
{
    /// <summary>
    /// The single home of transcendental math in the simulation (D-012). Every exp/log/logit/sigmoid goes
    /// through here so the implementation can be swapped in one place. Direct Math.Exp/Math.Log calls
    /// elsewhere in Sim and AI are banned by the analyzer (src/BannedSymbols.txt).
    /// </summary>
    public static class CheckMath
    {
        /// <summary>e^x.</summary>
        public static double Exp(double x)
        {
#pragma warning disable RS0030 // CheckMath is the one allowed caller.
            return Math.Exp(x);
#pragma warning restore RS0030
        }

        /// <summary>Natural logarithm.</summary>
        public static double Log(double x)
        {
#pragma warning disable RS0030 // CheckMath is the one allowed caller.
            return Math.Log(x);
#pragma warning restore RS0030
        }

        /// <summary>Square root.</summary>
        public static double Sqrt(double x)
        {
#pragma warning disable RS0030 // CheckMath is the one allowed caller.
            return Math.Sqrt(x);
#pragma warning restore RS0030
        }

        /// <summary>logit(p) = ln(p / (1 - p)). <paramref name="p"/> must be in (0, 1).</summary>
        public static double Logit(double p)
        {
            if (!(p > 0.0 && p < 1.0))
            {
                throw new ArgumentOutOfRangeException(nameof(p), p, "Logit is defined only for probabilities in (0, 1).");
            }

            return Log(p / (1.0 - p));
        }

        /// <summary>Logistic function 1 / (1 + e^-x), evaluated without overflow for large |x|.</summary>
        public static double Sigmoid(double x)
        {
            if (x >= 0.0)
            {
                return 1.0 / (1.0 + Exp(-x));
            }

            double e = Exp(x);
            return e / (1.0 + e);
        }

        /// <summary>Clamps <paramref name="value"/> into [<paramref name="min"/>, <paramref name="max"/>].</summary>
        public static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
