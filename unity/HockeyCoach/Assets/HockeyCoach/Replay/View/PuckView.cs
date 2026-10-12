using HockeyCoach.Replay.Core;
using UnityEngine;

namespace HockeyCoach.Replay.View
{
    /// <summary>The puck: a small dark disc on the sampled puck position, drawn above the players.</summary>
    public sealed class PuckView : MonoBehaviour
    {
        /// <summary>Builds the puck mesh on this GameObject.</summary>
        public void Build(ReplayGraphics graphics, ReplayViewStyle style)
        {
            Mesh mesh = graphics.Mesh(new MeshBuilder().Disc(Vector2.zero, style.puckRadius, style.circleSegments, style.puck), "ReplayPuck");
            graphics.Attach(gameObject, mesh, ReplayLayers.PuckOrder);
        }

        /// <summary>Moves the puck to <paramref name="position"/>.</summary>
        public void Show(RinkPoint position)
        {
            transform.localPosition = ReplayLayers.ToWorld(position, ReplayLayers.PuckZ);
        }
    }
}
