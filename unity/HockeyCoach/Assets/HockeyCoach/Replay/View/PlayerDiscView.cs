using HockeyCoach.Replay.Core;
using UnityEngine;

namespace HockeyCoach.Replay.View
{
    /// <summary>
    /// One roster player: a team-coloured disc (goalies in a darker shade) with a label of position and id. The
    /// GameObject sits exactly on the sampled rink position; the drawn disc and label are shifted by
    /// <see cref="ReplayViewStyle.teamShift"/> towards the team's own end so opponents on one node stay visible. The
    /// GameObject is inactive while the player is off the ice.
    /// </summary>
    public sealed class PlayerDiscView : MonoBehaviour
    {
        /// <summary>Player id.</summary>
        public int PlayerId { get; private set; }

        /// <summary>Team.</summary>
        public ReplayTeam Team { get; private set; }

        /// <summary>True for goalies.</summary>
        public bool IsGoalie { get; private set; }

        /// <summary>Label text (position and id).</summary>
        public string Label { get; private set; }

        /// <summary>True while the player is on the ice (the GameObject is active).</summary>
        public bool IsOnIce => gameObject.activeSelf;

        /// <summary>The label component, null when no font is available.</summary>
        public TextMesh LabelMesh { get; private set; }

        /// <summary>Builds the disc and label as children of this GameObject; starts hidden.</summary>
        public void Build(int id, ReplayTeam team, bool isGoalie, string label, ReplayGraphics graphics, ReplayViewStyle style)
        {
            PlayerId = id;
            Team = team;
            IsGoalie = isGoalie;
            Label = label;
            float shift = team == ReplayTeam.Home ? -style.teamShift : style.teamShift;

            var body = new GameObject("Disc");
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(shift, 0, 0);
            Color color = team == ReplayTeam.Home ? (isGoalie ? style.homeGoalie : style.home) : (isGoalie ? style.awayGoalie : style.away);
            Mesh mesh = graphics.Mesh(new MeshBuilder().Disc(Vector2.zero, style.discRadius, style.circleSegments, color), "ReplayDisc" + id);
            graphics.Attach(body, mesh, ReplayLayers.DiscOrder);

            if (graphics.Font != null)
            {
                var text = new GameObject("Label");
                text.transform.SetParent(transform, false);
                text.transform.localPosition = new Vector3(shift, 0, ReplayLayers.LabelZ);
                LabelMesh = text.AddComponent<TextMesh>();
                LabelMesh.font = graphics.Font;
                LabelMesh.text = label;
                LabelMesh.anchor = TextAnchor.MiddleCenter;
                LabelMesh.alignment = TextAlignment.Center;
                LabelMesh.characterSize = style.labelCharacterSize;
                LabelMesh.fontSize = style.labelFontSize;
                LabelMesh.color = style.label;
                var renderer = text.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = graphics.Font.material;
                renderer.sortingOrder = ReplayLayers.LabelOrder;
            }

            Hide();
        }

        /// <summary>Places the player on <paramref name="position"/> and shows him.</summary>
        public void Show(RinkPoint position)
        {
            transform.localPosition = ReplayLayers.ToWorld(position, ReplayLayers.DiscZ);
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        /// <summary>Hides the player (off the ice).</summary>
        public void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
