using HockeyCoach.Replay.Core;
using UnityEngine;

namespace HockeyCoach.Replay.View
{
    /// <summary>
    /// Head-up display drawn with IMGUI (no uGUI assets): score, period and clock, speed, play state, the current event
    /// and the controls. <see cref="Show"/> sets the texts from the cursor; <see cref="OnGUI"/> only draws them.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        private const string ControlsText = "Space play/pause   Left/Right step event   Up/Down or +/- speed   Home restart";

        private GUIStyle _style;

        /// <summary>Score line.</summary>
        public string ScoreText { get; private set; } = string.Empty;

        /// <summary>Period and clock.</summary>
        public string ClockText { get; private set; } = string.Empty;

        /// <summary>Playback speed.</summary>
        public string SpeedText { get; private set; } = string.Empty;

        /// <summary><c>Playing</c>, <c>Paused</c> or <c>End</c>.</summary>
        public string StateText { get; private set; } = string.Empty;

        /// <summary>Heading of the current event (index and type).</summary>
        public string EventHeadingText { get; private set; } = string.Empty;

        /// <summary>Description of the current event.</summary>
        public string EventText { get; private set; } = string.Empty;

        /// <summary>Index of the event shown.</summary>
        public int EventIndex { get; private set; } = -1;

        /// <summary>Updates the texts from the cursor and its sample.</summary>
        public void Show(ReplayCursor cursor, ReplaySample sample)
        {
            ReplayTimeline timeline = cursor.Timeline;
            TimelineSegment segment = timeline.Segments[sample.SegmentIndex];
            ScoreText = ReplayText.Score(sample.HomeScore, sample.AwayScore);
            ClockText = ReplayText.PeriodClock(sample.Period, sample.PeriodTime);
            SpeedText = ReplayText.Speed(cursor.Speed);
            StateText = cursor.IsPlaying ? "Playing" : cursor.IsAtEnd ? "End" : "Paused";
            EventIndex = sample.SegmentIndex;
            EventHeadingText = ReplayText.EventHeading(segment, timeline.Segments.Count);
            EventText = ReplayText.Describe(segment, timeline);
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(14, Screen.height / 45), wordWrap = true };
                _style.normal.textColor = Color.white;
            }

            float line = _style.fontSize * 1.5f;
            float width = Mathf.Max(200, Screen.width - 20);
            var area = new Rect(10, 10, width, line * 5.5f);
            GUI.Box(area, GUIContent.none);
            float y = area.y + 4;
            GUI.Label(new Rect(18, y, width - 16, line), ScoreText + "     " + ClockText + "     " + SpeedText + "  " + StateText, _style);
            y += line;
            GUI.Label(new Rect(18, y, width - 16, line), EventHeadingText, _style);
            y += line;
            GUI.Label(new Rect(18, y, width - 16, line * 2), EventText, _style);
            y += line * 2;
            GUI.Label(new Rect(18, y, width - 16, line), ControlsText, _style);
        }
    }
}
