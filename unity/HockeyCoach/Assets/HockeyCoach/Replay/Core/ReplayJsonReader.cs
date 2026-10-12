using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// Reads a <c>match --json</c> export into a <see cref="ReplayMatch"/>. Checks the format version first, then the
    /// parts the replay needs (header, rink, events with placements); every failure is a <see cref="ReplayFormatException"/>
    /// with a message that names the problem.
    /// </summary>
    public static class ReplayJsonReader
    {
        /// <summary>The only <c>formatVersion</c> this reader understands.</summary>
        public const int SupportedFormatVersion = 1;

        /// <summary>Parses and validates the JSON text.</summary>
        /// <exception cref="ReplayFormatException">The text is not a replayable match export.</exception>
        public static ReplayMatch Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ReplayFormatException("Match JSON is empty.");
            }

            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (JsonException e)
            {
                throw new ReplayFormatException("Match JSON is not a valid JSON object: " + e.Message, e);
            }

            CheckFormatVersion(root);
            ReplayMatch match;
            try
            {
                match = root.ToObject<ReplayMatch>(JsonSerializer.Create(new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                }));
            }
            catch (JsonException e)
            {
                throw new ReplayFormatException("Match JSON does not match the export format: " + e.Message, e);
            }
            catch (ArgumentException e)
            {
                throw new ReplayFormatException("Match JSON does not match the export format: " + e.Message, e);
            }

            Validate(match);
            return match;
        }

        private static void CheckFormatVersion(JObject root)
        {
            JToken token = root["formatVersion"];
            if (token == null || token.Type == JTokenType.Null)
            {
                throw new ReplayFormatException(
                    "Match JSON has no formatVersion: it is not a 'match --json' export with a replay header (D-087). Re-export the match with the current Harness.");
            }

            if (token.Type != JTokenType.Integer || token.Value<long>() != SupportedFormatVersion)
            {
                throw new ReplayFormatException(
                    "Unsupported match JSON formatVersion " + token.ToString(Formatting.None) + "; this replay reads formatVersion " + SupportedFormatVersion + ".");
            }
        }

        private static void Validate(ReplayMatch match)
        {
            if (match.PeriodSeconds <= 0 || match.PeriodCount <= 0)
            {
                throw new ReplayFormatException("Match JSON header needs positive periodSeconds and periodCount.");
            }

            if (match.Roster == null)
            {
                throw new ReplayFormatException("Match JSON header has no roster.");
            }

            ValidateRink(match.Rink);
            if (match.Events == null || match.Events.Count == 0)
            {
                throw new ReplayFormatException("Match JSON has no events.");
            }

            if (match.Goals == null)
            {
                match.Goals = new List<GoalNoteData>();
            }

            int nodeCount = match.Rink.Length * match.Rink.Width;
            for (int i = 0; i < match.Events.Count; i++)
            {
                ReplayEvent e = match.Events[i];
                if (e == null)
                {
                    throw new ReplayFormatException("Event " + i + " is null.");
                }

                ReplayEventType kind;
                if (e.Type == null || !Enum.TryParse(e.Type, false, out kind) || !Enum.IsDefined(typeof(ReplayEventType), kind) || IsNumeric(e.Type))
                {
                    throw new ReplayFormatException("Event " + i + " has unknown type '" + e.Type + "'.");
                }

                e.Kind = kind;
                if (e.Placement == null || e.Placement.Players == null)
                {
                    throw new ReplayFormatException("Event " + i + " (" + e.Type + ") has no placement.");
                }

                CheckNode(e.Placement.PuckNodeId, nodeCount, i, "puck");
                foreach (PlacedPlayerData p in e.Placement.Players)
                {
                    if (p == null)
                    {
                        throw new ReplayFormatException("Event " + i + " has a null placement player.");
                    }

                    CheckNode(p.NodeId, nodeCount, i, "player " + p.Id);
                    ReplayTeams.Parse(p.Team);
                }

                if (e.Kind == ReplayEventType.LineChange && (e.OutgoingIds == null || e.IncomingIds == null))
                {
                    throw new ReplayFormatException("Event " + i + " (LineChange) has no outgoingIds or incomingIds.");
                }

                if ((e.Kind == ReplayEventType.Pass && e.PasserId == null) || (e.Kind == ReplayEventType.Shot && e.ShooterId == null))
                {
                    throw new ReplayFormatException("Event " + i + " (" + e.Type + ") has no actor id.");
                }
            }
        }

        private static bool IsNumeric(string value)
        {
            return value.Length > 0 && (char.IsDigit(value[0]) || value[0] == '-' || value[0] == '+');
        }

        private static void CheckNode(int node, int nodeCount, int index, string what)
        {
            if (node < 0 || node >= nodeCount)
            {
                throw new ReplayFormatException("Event " + index + ": " + what + " node " + node + " is outside the rink grid (0–" + (nodeCount - 1) + ").");
            }
        }

        private static void ValidateRink(RinkData rink)
        {
            if (rink == null)
            {
                throw new ReplayFormatException("Match JSON header has no rink.");
            }

            if (rink.Length < 4 || rink.Width < 1)
            {
                throw new ReplayFormatException("Rink grid " + rink.Length + " x " + rink.Width + " is too small (length must be at least 4).");
            }

            if (rink.OwnGoal == null || rink.OpponentGoal == null)
            {
                throw new ReplayFormatException("Rink has no ownGoal or opponentGoal.");
            }

            if (rink.FaceoffSpots == null)
            {
                throw new ReplayFormatException("Rink has no faceoffSpots.");
            }

            RinkMetresData m = rink.Metres;
            if (m == null)
            {
                throw new ReplayFormatException("Rink has no metres (Q-077).");
            }

            if (!(m.Length > 0) || !(m.Width > 0) || !(m.GoalLineFromEnd > 0) || !(m.GoalLineFromEnd * 2 < m.Length)
                || !(m.BlueLineFromEnd > m.GoalLineFromEnd) || !(m.BlueLineFromEnd * 2 < m.Length))
            {
                throw new ReplayFormatException("Rink metres are inconsistent: need 0 < goalLineFromEnd < blueLineFromEnd < length / 2 and a positive width.");
            }
        }
    }
}
