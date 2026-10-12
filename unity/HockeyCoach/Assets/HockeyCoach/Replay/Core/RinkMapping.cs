using System;
using System.Collections.Generic;

namespace HockeyCoach.Replay.Core
{
    /// <summary>A named rink spot in metres (faceoff spot).</summary>
    public sealed class RinkSpot
    {
        /// <summary>Creates a spot.</summary>
        public RinkSpot(string id, int nodeId, RinkPoint position)
        {
            Id = id;
            NodeId = nodeId;
            Position = position;
        }

        /// <summary>Spot id from the rink data, e.g. <c>center</c>.</summary>
        public string Id { get; }

        /// <summary>Grid node of the spot.</summary>
        public int NodeId { get; }

        /// <summary>Position in metres.</summary>
        public RinkPoint Position { get; }
    }

    /// <summary>
    /// Maps grid nodes to metres and exposes the rink lines for the view, from the header's <c>rink</c>
    /// (<c>metres</c>, Q-077). Rows x = 1 and x = length − 2 lie on the goal lines (<c>goalLineFromEnd</c> from each end);
    /// the rows between them are evenly spaced; the end rows x = 0 and x = length − 1 lie halfway between the end boards
    /// and the goal line. Lane y is the centre of the y-th of <c>width</c> equal lanes. The home team attacks +x.
    /// </summary>
    public sealed class RinkMapping
    {
        private readonly RinkSpot[] _faceoffSpots;

        /// <summary>Creates the mapping; the rink must have been validated by <see cref="ReplayJsonReader"/> (or be equally complete).</summary>
        public RinkMapping(RinkData rink)
        {
            if (rink == null)
            {
                throw new ArgumentNullException(nameof(rink));
            }

            if (rink.Metres == null || rink.Length < 4 || rink.Width < 1)
            {
                throw new ReplayFormatException("Rink needs metres and a grid of at least 4 rows.");
            }

            GridLength = rink.Length;
            GridWidth = rink.Width;
            LengthMetres = rink.Metres.Length;
            WidthMetres = rink.Metres.Width;
            GoalLineFromEnd = rink.Metres.GoalLineFromEnd;
            BlueLineFromEnd = rink.Metres.BlueLineFromEnd;
            RowSpacing = (LengthMetres - (2 * GoalLineFromEnd)) / (GridLength - 3);
            LaneWidth = WidthMetres / GridWidth;
            HomeNet = GridPosition(rink.OwnGoal.X, rink.OwnGoal.Y);
            AwayNet = GridPosition(rink.OpponentGoal.X, rink.OpponentGoal.Y);
            HomeNetNodeId = NodeId(rink.OwnGoal.X, rink.OwnGoal.Y);
            AwayNetNodeId = NodeId(rink.OpponentGoal.X, rink.OpponentGoal.Y);
            var spots = new List<RinkSpot>();
            if (rink.FaceoffSpots != null)
            {
                foreach (FaceoffSpotData s in rink.FaceoffSpots)
                {
                    spots.Add(new RinkSpot(s.Id, s.NodeId, NodePosition(s.NodeId)));
                }
            }

            _faceoffSpots = spots.ToArray();
        }

        /// <summary>Grid rows along the rink.</summary>
        public int GridLength { get; }

        /// <summary>Grid lanes across the rink.</summary>
        public int GridWidth { get; }

        /// <summary>Number of nodes (<see cref="GridLength"/> × <see cref="GridWidth"/>).</summary>
        public int NodeCount => GridLength * GridWidth;

        /// <summary>Rink length in metres.</summary>
        public double LengthMetres { get; }

        /// <summary>Rink width in metres.</summary>
        public double WidthMetres { get; }

        /// <summary>Distance from each end board to its goal line.</summary>
        public double GoalLineFromEnd { get; }

        /// <summary>Distance from each end board to the near edge of its blue line.</summary>
        public double BlueLineFromEnd { get; }

        /// <summary>Metres between adjacent rows from goal line to goal line.</summary>
        public double RowSpacing { get; }

        /// <summary>Width of one lane in metres.</summary>
        public double LaneWidth { get; }

        /// <summary>X of the goal line at the home end (the home team's own net).</summary>
        public double HomeGoalLineX => GoalLineFromEnd;

        /// <summary>X of the goal line at the away end (the away team's own net).</summary>
        public double AwayGoalLineX => LengthMetres - GoalLineFromEnd;

        /// <summary>X of the near edge (seen from the home end) of the home team's blue line.</summary>
        public double HomeBlueLineX => BlueLineFromEnd;

        /// <summary>X of the near edge (seen from the away end) of the away team's blue line.</summary>
        public double AwayBlueLineX => LengthMetres - BlueLineFromEnd;

        /// <summary>X of the centre line.</summary>
        public double CentreLineX => LengthMetres / 2;

        /// <summary>Goal mouth of the home team's own net (where the home team concedes).</summary>
        public RinkPoint HomeNet { get; }

        /// <summary>Goal mouth of the away team's own net (where the away team concedes).</summary>
        public RinkPoint AwayNet { get; }

        /// <summary>Grid node of the home team's own net.</summary>
        public int HomeNetNodeId { get; }

        /// <summary>Grid node of the away team's own net.</summary>
        public int AwayNetNodeId { get; }

        /// <summary>Faceoff spots in the rink data's order.</summary>
        public IReadOnlyList<RinkSpot> FaceoffSpots => _faceoffSpots;

        /// <summary>The own net of <paramref name="team"/>, i.e. where that team concedes.</summary>
        public RinkPoint NetOf(ReplayTeam team)
        {
            return team == ReplayTeam.Home ? HomeNet : AwayNet;
        }

        /// <summary>The grid node of the own net of <paramref name="team"/>.</summary>
        public int NetNodeOf(ReplayTeam team)
        {
            return team == ReplayTeam.Home ? HomeNetNodeId : AwayNetNodeId;
        }

        /// <summary>The faceoff spot on <paramref name="nodeId"/>, or null.</summary>
        public RinkSpot FaceoffSpotAt(int nodeId)
        {
            foreach (RinkSpot spot in _faceoffSpots)
            {
                if (spot.NodeId == nodeId)
                {
                    return spot;
                }
            }

            return null;
        }

        /// <summary>Node id of a grid point (x * width + y).</summary>
        public int NodeId(int x, int y)
        {
            return (x * GridWidth) + y;
        }

        /// <summary>Position in metres of a node.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The id is outside the grid.</exception>
        public RinkPoint NodePosition(int nodeId)
        {
            if (nodeId < 0 || nodeId >= NodeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeId), nodeId, "Node is outside the rink grid.");
            }

            return GridPosition(nodeId / GridWidth, nodeId % GridWidth);
        }

        /// <summary>Position in metres of a grid point.</summary>
        public RinkPoint GridPosition(int x, int y)
        {
            return new RinkPoint(RowX(x), LaneY(y));
        }

        /// <summary>X in metres of grid row <paramref name="x"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The row is outside the grid.</exception>
        public double RowX(int x)
        {
            if (x < 0 || x >= GridLength)
            {
                throw new ArgumentOutOfRangeException(nameof(x), x, "Row is outside the rink grid.");
            }

            if (x == 0)
            {
                return GoalLineFromEnd / 2;
            }

            if (x == GridLength - 1)
            {
                return LengthMetres - (GoalLineFromEnd / 2);
            }

            return GoalLineFromEnd + ((x - 1) * RowSpacing);
        }

        /// <summary>Y in metres of lane <paramref name="y"/> (the lane centre).</summary>
        /// <exception cref="ArgumentOutOfRangeException">The lane is outside the grid.</exception>
        public double LaneY(int y)
        {
            if (y < 0 || y >= GridWidth)
            {
                throw new ArgumentOutOfRangeException(nameof(y), y, "Lane is outside the rink grid.");
            }

            return (y + 0.5) * LaneWidth;
        }
    }
}
