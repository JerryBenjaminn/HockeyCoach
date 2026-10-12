using System;
using UnityEngine;

namespace HockeyCoach.Replay.View
{
    /// <summary>
    /// Look of the replay: colours, sizes in metres and draw depths. Purely visual; nothing here affects what the replay
    /// shows happening. Serialized on <see cref="ReplayBootstrap"/> so it can be tuned in the Inspector.
    /// </summary>
    [Serializable]
    public sealed class ReplayViewStyle
    {
        [Header("Camera")]
        [Tooltip("Free space around the rink in metres.")]
        public float cameraMargin = 3f;

        [Tooltip("Camera background colour.")]
        public Color background = new Color(0.12f, 0.13f, 0.15f);

        [Header("Rink")]
        public Color ice = new Color(0.93f, 0.96f, 1f);
        public Color boards = new Color(0.25f, 0.25f, 0.28f);
        public Color redLine = new Color(0.8f, 0.1f, 0.12f);
        public Color blueLine = new Color(0.12f, 0.25f, 0.75f);
        public Color net = new Color(0.55f, 0.05f, 0.08f);

        [Tooltip("Board thickness in metres (drawn outside the rink).")]
        public float boardThickness = 0.4f;

        [Tooltip("Centre line width in metres.")]
        public float centreLineWidth = 0.3f;

        [Tooltip("Blue line width in metres (drawn from blueLineFromEnd towards the centre).")]
        public float blueLineWidth = 0.3f;

        [Tooltip("Goal line width in metres.")]
        public float goalLineWidth = 0.1f;

        [Tooltip("Faceoff spot radius in metres.")]
        public float faceoffSpotRadius = 0.3f;

        [Tooltip("Net width (across the rink) in metres.")]
        public float netWidth = 1.83f;

        [Tooltip("Net depth (behind the goal line) in metres.")]
        public float netDepth = 1.12f;

        [Tooltip("Net frame line width in metres.")]
        public float netLineWidth = 0.12f;

        [Header("Players")]
        public Color home = new Color(0.1f, 0.45f, 0.95f);
        public Color homeGoalie = new Color(0.03f, 0.18f, 0.45f);
        public Color away = new Color(0.95f, 0.55f, 0.1f);
        public Color awayGoalie = new Color(0.55f, 0.27f, 0.02f);
        public Color label = Color.white;

        [Tooltip("Player disc radius in metres.")]
        public float discRadius = 1.1f;

        [Tooltip("Sideways shift in metres of the drawn disc along the rink: home towards its own end, away towards its own end, so opponents on one node stay visible (Q-080). 0 draws both exactly on the node.")]
        public float teamShift = 0.9f;

        [Tooltip("TextMesh character size of the disc labels.")]
        public float labelCharacterSize = 0.12f;

        [Tooltip("TextMesh font size of the disc labels.")]
        public int labelFontSize = 48;

        [Header("Puck")]
        public Color puck = new Color(0.05f, 0.05f, 0.05f);

        [Tooltip("Puck radius in metres (drawn larger than a real puck so it stays visible).")]
        public float puckRadius = 0.4f;

        [Header("Mesh detail")]
        [Tooltip("Segments of a drawn circle.")]
        public int circleSegments = 32;
    }
}
