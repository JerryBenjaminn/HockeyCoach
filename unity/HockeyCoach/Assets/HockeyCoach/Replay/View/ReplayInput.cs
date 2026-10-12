using HockeyCoach.Replay.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HockeyCoach.Replay.View
{
    /// <summary>
    /// Keyboard controls (Input System): Space play/pause, Right/Left step to the next/previous event, Up/Down or +/-
    /// change speed along the <see cref="SpeedLadder"/>, Home restart. The actions are public so tests can call them
    /// without a keyboard; every action refreshes the view.
    /// </summary>
    public sealed class ReplayInput : MonoBehaviour
    {
        private ReplayBootstrap _bootstrap;

        /// <summary>The selectable speeds.</summary>
        public SpeedLadder Speeds { get; private set; }

        /// <summary>Connects the controls to a loaded replay and applies the ladder's speed to the cursor.</summary>
        public void Bind(ReplayBootstrap bootstrap, SpeedLadder speeds)
        {
            _bootstrap = bootstrap;
            Speeds = speeds;
            bootstrap.Cursor.Speed = speeds.Current;
        }

        /// <summary>Play or pause.</summary>
        public void TogglePlay()
        {
            Do(c => c.TogglePlay());
        }

        /// <summary>Jump to the state after the next event.</summary>
        public void StepNext()
        {
            Do(c => c.StepNext());
        }

        /// <summary>Jump to the state after the previous event.</summary>
        public void StepPrevious()
        {
            Do(c => c.StepPrevious());
        }

        /// <summary>Next higher speed.</summary>
        public void Faster()
        {
            Do(c => c.Speed = Speeds.Faster());
        }

        /// <summary>Next lower speed.</summary>
        public void Slower()
        {
            Do(c => c.Speed = Speeds.Slower());
        }

        /// <summary>Back to the start of the match; the play state is kept.</summary>
        public void Restart()
        {
            Do(c => c.SeekClock(c.Timeline.StartClock));
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || _bootstrap == null || _bootstrap.Cursor == null)
            {
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                TogglePlay();
            }

            if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                StepNext();
            }

            if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                StepPrevious();
            }

            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame || keyboard.equalsKey.wasPressedThisFrame)
            {
                Faster();
            }

            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame || keyboard.minusKey.wasPressedThisFrame)
            {
                Slower();
            }

            if (keyboard.homeKey.wasPressedThisFrame)
            {
                Restart();
            }
        }

        private void Do(System.Action<ReplayCursor> action)
        {
            if (_bootstrap == null || _bootstrap.Cursor == null)
            {
                return;
            }

            action(_bootstrap.Cursor);
            _bootstrap.Refresh();
        }
    }
}
