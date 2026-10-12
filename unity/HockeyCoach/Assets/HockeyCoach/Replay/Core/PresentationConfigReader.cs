using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// Strict reader of <c>data/replay.json</c> (D-088 B, data-schema.md, replay.json). Rejects the file when it is not a
    /// JSON object, when <c>schemaVersion</c> is not <see cref="SupportedSchemaVersion"/>, when a key is missing, when a
    /// section has an unknown key (keys starting with <c>_</c> are meta keys and ignored) or when a value is not a finite
    /// number in its range. Every failure is a <see cref="PresentationConfigException"/> naming the key path. There are no
    /// silent defaults. The ranges that depend on the rink (bench gates) are checked by <see cref="CheckRink"/>.
    /// </summary>
    public static class PresentationConfigReader
    {
        /// <summary>The only <c>schemaVersion</c> this reader understands.</summary>
        public const int SupportedSchemaVersion = 1;

        /// <summary>Goal depth that bounds <c>goal.netInsideMetres</c> (data-schema.md, replay.json, Avaimet).</summary>
        public const double NetDepthMetres = 1.12;

        /// <summary>
        /// Distance outside the boards that bounds <c>bench.gateYMetres</c> (data-schema.md: hylätään, jos &lt; −3 tai
        /// &gt; kaukalon leveys + 3).
        /// </summary>
        public const double GateYMarginMetres = 3.0;

        private static readonly string[] FloorModes = { "allLegs", "firstLeg" };

        private static readonly string[] TopLevelKeys = { "schemaVersion", "timing", "goal", "puck", "skaters", "goalies", "bench" };

        /// <summary>Parses and validates the text of <c>replay.json</c>.</summary>
        /// <exception cref="PresentationConfigException">The text is not a valid presentation config.</exception>
        public static PresentationConfig Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new PresentationConfigException("replay.json is empty.");
            }

            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (JsonException e)
            {
                throw new PresentationConfigException("replay.json is not a valid JSON object: " + e.Message, e);
            }

            CheckSchemaVersion(root);
            CheckKnownKeys(root, null, TopLevelKeys);

            var timing = new TimingSettings();
            Section s = Section.Of(root, "timing");
            timing.ActionCompression = s.Number("actionCompression", 0, true, 20);
            timing.DeadCompression = s.Number("deadCompression", timing.ActionCompression, false, 50);
            timing.MinEventSeconds = s.Number("minEventSeconds", 0, false, 5);
            timing.LineChangeSeconds = s.Number("lineChangeSeconds", 0, false, 5);
            timing.StoppageHoldSeconds = s.Number("stoppageHoldSeconds", 0, false, 10);
            timing.TimeoutHoldSeconds = s.Number("timeoutHoldSeconds", 0, false, 10);
            timing.PeriodBreakSeconds = s.Number("periodBreakSeconds", 0, false, 30);
            timing.FaceoffApproachSpeed = s.Number("faceoffApproachSpeed", 0, true, 15);
            timing.FaceoffApproachMaxSeconds = s.Number("faceoffApproachMaxSeconds", 0, true, 10);
            timing.FaceoffApproachMinSeconds = s.Number("faceoffApproachMinSeconds", 0, false, timing.FaceoffApproachMaxSeconds);
            s.CheckUnknown();

            var goal = new GoalSettings();
            s = Section.Of(root, "goal");
            goal.SlowMoRate = s.Number("slowMoRate", 0, true, 1);
            goal.SlowMoMinSeconds = s.Number("slowMoMinSeconds", 0, false, 10);
            goal.HoldSeconds = s.Number("holdSeconds", 0, false, 10);
            goal.NetInsideMetres = s.Number("netInsideMetres", 0, false, NetDepthMetres);
            s.CheckUnknown();

            var puck = new PuckSettings();
            s = Section.Of(root, "puck");
            puck.PassSpeed = s.Number("passSpeed", 5, false, 50);
            puck.LongPassSpeed = s.Number("longPassSpeed", puck.PassSpeed, false, 50);
            puck.LongPassMinMetres = s.Number("longPassMinMetres", 0, true, double.PositiveInfinity);
            puck.ShotSpeed = s.Number("shotSpeed", 10, false, 50);
            puck.DumpInSpeed = s.Number("dumpInSpeed", 5, false, 50);
            puck.ReboundSpeed = s.Number("reboundSpeed", 0, true, 50);
            puck.BlockDeflectSpeed = s.Number("blockDeflectSpeed", 0, true, 50);
            puck.LoosePuckSpeed = s.Number("loosePuckSpeed", 0, true, 50);
            puck.FaceoffDrawSpeed = s.Number("faceoffDrawSpeed", 0, true, 50);
            puck.FaceoffDrawDelaySeconds = s.Number("faceoffDrawDelaySeconds", 0, false, 2);
            puck.MissWideMetres = s.Number("missWideMetres", 0, false, 5);
            puck.CarriedLeadMetres = s.Number("carriedLeadMetres", 0, false, 2);
            puck.CarriedHeadingMinSpeed = s.Number("carriedHeadingMinSpeed", 0, false, double.PositiveInfinity);
            puck.FloorMode = s.Choice("floorMode", FloorModes) == "firstLeg" ? PuckFloorMode.FirstLeg : PuckFloorMode.AllLegs;
            s.CheckUnknown();

            var skaters = new SkaterSettings();
            s = Section.Of(root, "skaters");
            skaters.TopSpeed = s.Number("topSpeed", 3, false, 12);
            skaters.Acceleration = s.Number("acceleration", 0, true, 20);
            skaters.Deceleration = s.Number("deceleration", 0, true, 20);
            skaters.PathAlpha = s.Number("pathAlpha", 0, false, 1);
            skaters.PathTension = s.Number("pathTension", 0, false, 1);
            skaters.StopTurnDegrees = s.Number("stopTurnDegrees", 0, false, 180);
            skaters.AnticipationSeconds = s.Number("anticipationSeconds", 0, false, 2);
            skaters.NodeOffsetRadiusMetres = s.Number("nodeOffsetRadiusMetres", 0, false, 2.5);
            skaters.SeparationMinMetres = s.Number("separationMinMetres", 0, false, 4);
            skaters.SeparationIterations = s.Integer("separationIterations", 0, 10);
            skaters.IdleAmplitudeMetres = s.Number("idleAmplitudeMetres", 0, false, 1);
            skaters.IdleFrequencyHz = s.Number("idleFrequencyHz", 0, false, 3);
            skaters.IdleYFrequencyRatio = s.Number("idleYFrequencyRatio", 0, true, 3);
            s.CheckUnknown();

            var goalies = new GoalieSettings();
            s = Section.Of(root, "goalies");
            goalies.DepthMetres = s.Number("depthMetres", 0, false, 3);
            goalies.LateralTrackFactor = s.Number("lateralTrackFactor", 0, false, 1);
            goalies.LateralMaxMetres = s.Number("lateralMaxMetres", 0, false, 2);
            s.CheckUnknown();

            var bench = new BenchSettings();
            s = Section.Of(root, "bench");
            bench.HomeGateFromCentreMetres = s.Number("homeGateFromCentreMetres", double.NegativeInfinity, false, double.PositiveInfinity);
            bench.AwayGateFromCentreMetres = s.Number("awayGateFromCentreMetres", double.NegativeInfinity, false, double.PositiveInfinity);
            bench.GateYMetres = s.Number("gateYMetres", double.NegativeInfinity, false, double.PositiveInfinity);
            s.CheckUnknown();

            return new PresentationConfig(timing, goal, puck, skaters, goalies, bench);
        }

        /// <summary>
        /// Checks the ranges that depend on the rink: <c>bench.homeGateFromCentreMetres</c> and
        /// <c>bench.awayGateFromCentreMetres</c> at most half the rink length from the centre line, <c>bench.gateYMetres</c>
        /// within <see cref="GateYMarginMetres"/> of the boards.
        /// </summary>
        /// <exception cref="PresentationConfigException">A bench value does not fit the rink.</exception>
        public static void CheckRink(PresentationConfig config, RinkMetresData rink)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (rink == null)
            {
                throw new ArgumentNullException(nameof(rink));
            }

            double half = rink.Length / 2;
            CheckGate("bench.homeGateFromCentreMetres", config.Bench.HomeGateFromCentreMetres, half);
            CheckGate("bench.awayGateFromCentreMetres", config.Bench.AwayGateFromCentreMetres, half);
            double y = config.Bench.GateYMetres;
            if (y < -GateYMarginMetres || y > rink.Width + GateYMarginMetres)
            {
                throw new PresentationConfigException("replay.json: bench.gateYMetres is " + Format(y) + ", must be ≥ " + Format(-GateYMarginMetres)
                    + " and ≤ rink width + " + Format(GateYMarginMetres) + " (" + Format(rink.Width + GateYMarginMetres) + ").");
            }
        }

        private static void CheckGate(string path, double value, double half)
        {
            if (Math.Abs(value) > half)
            {
                throw new PresentationConfigException("replay.json: " + path + " is " + Format(value) + ", its absolute value must be ≤ half the rink length (" + Format(half) + ").");
            }
        }

        private static void CheckSchemaVersion(JObject root)
        {
            JToken token = root["schemaVersion"];
            if (token == null || token.Type == JTokenType.Null)
            {
                throw new PresentationConfigException("replay.json: missing key schemaVersion.");
            }

            if (token.Type != JTokenType.Integer || token.Value<long>() != SupportedSchemaVersion)
            {
                throw new PresentationConfigException("replay.json: schemaVersion is " + token.ToString(Formatting.None) + ", this replay reads schemaVersion " + SupportedSchemaVersion + ".");
            }
        }

        private static void CheckKnownKeys(JObject obj, string section, string[] known)
        {
            var allowed = new HashSet<string>(known, StringComparer.Ordinal);
            foreach (JProperty p in obj.Properties())
            {
                if (!p.Name.StartsWith("_", StringComparison.Ordinal) && !allowed.Contains(p.Name))
                {
                    throw new PresentationConfigException("replay.json: unknown key " + Path(section, p.Name) + ".");
                }
            }
        }

        private static string Path(string section, string key)
        {
            return section == null ? key : section + "." + key;
        }

        private static string Format(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        /// <summary>One section of the file; records the keys read so that the rest are unknown.</summary>
        private sealed class Section
        {
            private readonly JObject _obj;
            private readonly string _name;
            private readonly List<string> _read = new List<string>();

            private Section(JObject obj, string name)
            {
                _obj = obj;
                _name = name;
            }

            public static Section Of(JObject root, string name)
            {
                JToken token = root[name];
                if (token == null || token.Type == JTokenType.Null)
                {
                    throw new PresentationConfigException("replay.json: missing key " + name + ".");
                }

                JObject obj = token as JObject;
                if (obj == null)
                {
                    throw new PresentationConfigException("replay.json: " + name + " must be an object.");
                }

                return new Section(obj, name);
            }

            /// <summary>Reads a finite number; rejected when below <paramref name="min"/> (or equal, when <paramref name="minExclusive"/>) or above <paramref name="max"/>.</summary>
            public double Number(string key, double min, bool minExclusive, double max)
            {
                string path = Path(_name, key);
                _read.Add(key);
                JToken token = _obj[key];
                if (token == null || token.Type == JTokenType.Null)
                {
                    throw new PresentationConfigException("replay.json: missing key " + path + ".");
                }

                if (token.Type != JTokenType.Integer && token.Type != JTokenType.Float)
                {
                    throw new PresentationConfigException("replay.json: " + path + " must be a number, got " + token.ToString(Formatting.None) + ".");
                }

                double value = token.Value<double>();
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    throw new PresentationConfigException("replay.json: " + path + " must be a finite number, got " + token.ToString(Formatting.None) + ".");
                }

                bool low = minExclusive ? value <= min : value < min;
                if (low || value > max)
                {
                    string lower = double.IsNegativeInfinity(min) ? null : (minExclusive ? "> " : "≥ ") + Format(min);
                    string upper = double.IsPositiveInfinity(max) ? null : "≤ " + Format(max);
                    string rule = lower != null && upper != null ? lower + " and " + upper : lower ?? upper;
                    throw new PresentationConfigException("replay.json: " + path + " is " + Format(value) + ", must be " + rule + ".");
                }

                return value;
            }

            /// <summary>Reads a string that must be one of <paramref name="allowed"/>.</summary>
            public string Choice(string key, string[] allowed)
            {
                string path = Path(_name, key);
                _read.Add(key);
                JToken token = _obj[key];
                if (token == null || token.Type == JTokenType.Null)
                {
                    throw new PresentationConfigException("replay.json: missing key " + path + ".");
                }

                string value = token.Type == JTokenType.String ? token.Value<string>() : null;
                if (value == null || Array.IndexOf(allowed, value) < 0)
                {
                    throw new PresentationConfigException("replay.json: " + path + " is " + token.ToString(Formatting.None) + ", must be one of \"" + string.Join("\", \"", allowed) + "\".");
                }

                return value;
            }

            /// <summary>Reads a whole number in [<paramref name="min"/>, <paramref name="max"/>].</summary>
            public int Integer(string key, int min, int max)
            {
                double value = Number(key, min, false, max);
                if (value != Math.Floor(value))
                {
                    throw new PresentationConfigException("replay.json: " + Path(_name, key) + " is " + Format(value) + ", must be a whole number.");
                }

                return (int)value;
            }

            public void CheckUnknown()
            {
                CheckKnownKeys(_obj, _name, _read.ToArray());
            }
        }
    }
}
