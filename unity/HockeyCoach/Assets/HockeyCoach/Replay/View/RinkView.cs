using HockeyCoach.Replay.Core;
using UnityEngine;

namespace HockeyCoach.Replay.View
{
    /// <summary>
    /// The rink drawn from the replay header's metres (<see cref="RinkMapping"/>): ice, boards, goal lines, blue lines,
    /// centre line, faceoff spots and both nets, as one runtime-generated vertex-colour mesh in world metres.
    /// </summary>
    public sealed class RinkView : MonoBehaviour
    {
        /// <summary>The mapping drawn.</summary>
        public RinkMapping Mapping { get; private set; }

        /// <summary>Number of faceoff spots drawn.</summary>
        public int FaceoffSpotCount { get; private set; }

        /// <summary>Builds the rink mesh on this GameObject.</summary>
        public void Build(RinkMapping mapping, ReplayGraphics graphics, ReplayViewStyle style)
        {
            Mapping = mapping;
            float length = (float)mapping.LengthMetres;
            float width = (float)mapping.WidthMetres;
            var mesh = new MeshBuilder();

            float b = style.boardThickness;
            mesh.Rect(-b, -b, length + b, width + b, style.boards);
            mesh.Rect(0, 0, length, width, style.ice);

            VerticalLine(mesh, (float)mapping.HomeGoalLineX, style.goalLineWidth, width, style.redLine);
            VerticalLine(mesh, (float)mapping.AwayGoalLineX, style.goalLineWidth, width, style.redLine);
            VerticalLine(mesh, (float)mapping.CentreLineX, style.centreLineWidth, width, style.redLine);

            float home = (float)mapping.HomeBlueLineX;
            float away = (float)mapping.AwayBlueLineX;
            mesh.Rect(home, 0, home + style.blueLineWidth, width, style.blueLine);
            mesh.Rect(away - style.blueLineWidth, 0, away, width, style.blueLine);

            foreach (RinkSpot spot in mapping.FaceoffSpots)
            {
                mesh.Disc(new Vector2((float)spot.Position.X, (float)spot.Position.Y), style.faceoffSpotRadius, style.circleSegments, style.redLine);
            }

            FaceoffSpotCount = mapping.FaceoffSpots.Count;
            Net(mesh, mapping.HomeNet, mapping.CentreLineX, style);
            Net(mesh, mapping.AwayNet, mapping.CentreLineX, style);

            transform.localPosition = new Vector3(0, 0, ReplayLayers.RinkZ);
            graphics.Attach(gameObject, graphics.Mesh(mesh, "ReplayRink"), ReplayLayers.RinkOrder);
        }

        private static void VerticalLine(MeshBuilder mesh, float x, float lineWidth, float width, Color color)
        {
            mesh.Rect(x - (lineWidth / 2), 0, x + (lineWidth / 2), width, color);
        }

        /// <summary>A net as a three-sided frame behind the goal line, open towards the centre of the rink.</summary>
        private static void Net(MeshBuilder mesh, RinkPoint mouth, double centreX, ReplayViewStyle style)
        {
            float x = (float)mouth.X;
            float y = (float)mouth.Y;
            float back = mouth.X < centreX ? x - style.netDepth : x + style.netDepth;
            float half = style.netWidth / 2;
            float t = style.netLineWidth;
            float xMin = Mathf.Min(x, back);
            float xMax = Mathf.Max(x, back);
            mesh.Rect(xMin, y - half - (t / 2), xMax, y - half + (t / 2), style.net);
            mesh.Rect(xMin, y + half - (t / 2), xMax, y + half + (t / 2), style.net);
            mesh.Rect(back - (t / 2), y - half - (t / 2), back + (t / 2), y + half + (t / 2), style.net);
        }
    }
}
