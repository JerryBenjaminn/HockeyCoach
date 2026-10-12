using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HockeyCoach.Replay.Tests.EditMode
{
    /// <summary>
    /// A small match in the <c>match --json</c> format, built in code (no committed fixture): 11 × 5 grid like
    /// <c>data/rink.json</c> (home net node 7, away net node 47, centre spot 27), NHL metres, nine players per team
    /// (ids 1–9 home, 101–109 away; 6 and 106 are the goalies; 7–9 and 107–109 start on the bench).
    /// <list type="number">
    /// <item>0 s faceoff 2 vs 102 at the centre spot;</item>
    /// <item>5 s pass 4 → 2;</item>
    /// <item>9 s shot by 2 saved by 106;</item>
    /// <item>9 s stoppage;</item>
    /// <item>9 s line change, 1–3 out, 7–9 in;</item>
    /// <item>11 s faceoff 8 vs 102 at the centre spot.</item>
    /// </list>
    /// </summary>
    public static class SyntheticReplayJson
    {
        /// <summary>Placement after the first faceoff (event 0).</summary>
        public static readonly SortedDictionary<int, int> FaceoffPlacement = Placement(new[] { 1, 21, 2, 27, 3, 23, 4, 16, 5, 18, 6, 7, 101, 31, 102, 27, 103, 33, 104, 36, 105, 38, 106, 47 });

        /// <summary>Placement after the pass (events 1–3).</summary>
        public static readonly SortedDictionary<int, int> PassPlacement = Placement(new[] { 1, 26, 2, 32, 3, 28, 4, 16, 5, 18, 6, 7, 101, 31, 102, 22, 103, 33, 104, 36, 105, 38, 106, 47 });

        /// <summary>Placement after the line change (event 4).</summary>
        public static readonly SortedDictionary<int, int> ChangePlacement = Placement(new[] { 7, 21, 8, 22, 9, 23, 4, 16, 5, 18, 6, 7, 101, 31, 102, 27, 103, 33, 104, 36, 105, 38, 106, 47 });

        /// <summary>Placement after the second faceoff (event 5).</summary>
        public static readonly SortedDictionary<int, int> SecondFaceoffPlacement = Placement(new[] { 7, 21, 8, 27, 9, 23, 4, 16, 5, 18, 6, 7, 101, 31, 102, 27, 103, 33, 104, 36, 105, 38, 106, 47 });

        /// <summary>Puck node after the pass.</summary>
        public const int PassPuckNode = 32;

        /// <summary>Number of events.</summary>
        public const int EventCount = 6;

        private static readonly string[] Positions = { "LW", "C", "RW", "LD", "RD", "G", "LW", "C", "RW" };

        /// <summary>The match JSON.</summary>
        public static string Build()
        {
            var events = new List<string>
            {
                Event("Faceoff", 0, "\"homeCentreId\": 2, \"awayCentreId\": 102, \"winner\": \"Home\", \"location\": \"NeutralZone\"", 27, null, FaceoffPlacement),
                Event("Pass", 5, "\"passerId\": 4, \"receiverId\": 2, \"underPressure\": false, \"succeeded\": true", PassPuckNode, 2, PassPlacement),
                Event("Shot", 9, "\"shooterId\": 2, \"stoppedById\": 106, \"xg\": 0.05, \"chanceType\": \"OffensiveZone\", \"underPressure\": false, \"outcome\": \"Saved\"", PassPuckNode, 106, PassPlacement),
                Event("Stoppage", 9, "\"reason\": \"Freeze\"", PassPuckNode, null, PassPlacement),
                Event("LineChange", 9, "\"outgoingIds\": [1, 2, 3], \"incomingIds\": [7, 8, 9]", PassPuckNode, null, ChangePlacement),
                Event("Faceoff", 11, "\"homeCentreId\": 8, \"awayCentreId\": 102, \"winner\": \"Away\", \"location\": \"NeutralZone\"", 27, 102, SecondFaceoffPlacement),
            };

            var roster = new List<string>();
            foreach (string team in new[] { "Home", "Away" })
            {
                for (int i = 0; i < Positions.Length; i++)
                {
                    int id = (team == "Home" ? 0 : 100) + i + 1;
                    bool goalie = Positions[i] == "G";
                    roster.Add("{\"id\": " + id + ", \"team\": \"" + team + "\", \"position\": \"" + Positions[i] + "\", \"line\": null, \"pair\": null, \"goalie\": "
                        + (goalie ? "true, \"starter\": true" : "false, \"starter\": null") + "}");
                }
            }

            var json = new StringBuilder();
            json.Append("{\"seed\": 1, \"homeGoals\": 0, \"awayGoals\": 0, \"stalled\": false, \"formatVersion\": 2, \"periodSeconds\": 1200, \"periodCount\": 3, ");
            json.Append("\"time\": {\"secondsPerAction\": {\"breakout\": 4, \"deke\": 2, \"driveNet\": 2, \"dumpIn\": 2, \"faceoff\": 2, \"hit\": 2, \"loosePuck\": 3, ");
            json.Append("\"pass\": 2, \"rebound\": 1, \"shoot\": 1, \"skate\": 3, \"systemStep\": 2, \"zoneEntry\": 3}, \"setupSeconds\": 6, \"regroupSeconds\": 8}, ");
            json.Append("\"roster\": [").Append(string.Join(", ", roster)).Append("], ");
            json.Append("\"rink\": {\"length\": 11, \"width\": 5, \"ownGoal\": {\"x\": 1, \"y\": 2}, \"opponentGoal\": {\"x\": 9, \"y\": 2}, ");
            json.Append("\"faceoffSpots\": [{\"id\": \"defensiveLeft\", \"x\": 2, \"y\": 1, \"nodeId\": 11}, {\"id\": \"center\", \"x\": 5, \"y\": 2, \"nodeId\": 27}, {\"id\": \"offensiveRight\", \"x\": 8, \"y\": 3, \"nodeId\": 43}], ");
            json.Append("\"metres\": {\"length\": 60.96, \"width\": 25.91, \"goalLineFromEnd\": 3.35, \"blueLineFromEnd\": 22.86}}, ");
            json.Append("\"events\": [").Append(string.Join(", ", events)).Append("], \"goals\": []}");
            return json.ToString();
        }

        private static string Event(string type, int time, string fields, int puckNode, int? carrier, SortedDictionary<int, int> placement)
        {
            var players = new List<string>();
            foreach (KeyValuePair<int, int> p in placement)
            {
                players.Add("{\"id\": " + p.Key + ", \"team\": \"" + (p.Key > 100 ? "Away" : "Home") + "\", \"nodeId\": " + p.Value + ", \"goalie\": " + (p.Key % 100 == 6 ? "true" : "false") + "}");
            }

            return "{\"type\": \"" + type + "\", \"period\": 1, \"time\": " + time.ToString(CultureInfo.InvariantCulture) + ", \"strength\": \"5v5\", \"playId\": null, \"systemId\": null, "
                + fields + ", \"placement\": {\"puckCarrierId\": " + (carrier.HasValue ? carrier.Value.ToString(CultureInfo.InvariantCulture) : "null")
                + ", \"puckNodeId\": " + puckNode + ", \"players\": [" + string.Join(", ", players) + "]}}";
        }

        private static SortedDictionary<int, int> Placement(int[] pairs)
        {
            var result = new SortedDictionary<int, int>();
            for (int i = 0; i < pairs.Length; i += 2)
            {
                result[pairs[i]] = pairs[i + 1];
            }

            return result;
        }
    }
}
