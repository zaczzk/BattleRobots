using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Post-match replay playback controls.
    /// Reads <see cref="ReplaySO"/> to display a scrubber slider, current-time label,
    /// and Play / Pause / Stop buttons.
    ///
    /// ── Playback behaviour ─────────────────────────────────────────────────────
    ///   • Play  — sets <see cref="IsPlaying"/> true; <see cref="AdvancePlayback"/> must
    ///     be called each frame (e.g. from the owning replay scene controller) to move
    ///     the playhead forward. This keeps playback logic out of Update and avoids
    ///     a dedicated coroutine allocation.
    ///   • Pause — freezes the playhead without resetting it.
    ///   • Stop  — resets the playhead to 0 and deactivates the panel.
    ///   • Scrubber — user can drag the Slider; <see cref="OnSliderChanged"/> performs
    ///     one <see cref="ReplaySO.Seek"/> and one time-label string format per drag event
    ///     (not per-frame).
    ///
    /// ── Architecture ──────────────────────────────────────────────────────────
    ///   • <c>BattleRobots.UI</c> namespace — no BattleRobots.Physics references.
    ///   • No Update/FixedUpdate on this component — driven externally via
    ///     <see cref="AdvancePlayback"/> to stay zero-alloc.
    ///   • Panel shown/hidden via <see cref="OnReplayReady"/> (wired to
    ///     <c>ReplaySO._onReplayReady</c> VoidGameEvent via a VoidGameEventListener).
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_replay</c> SO in the Inspector.
    ///   2. Wire child UI elements (_playButton, _pauseButton, _stopButton,
    ///      _scrubber, _timeLabel) to their Button / Slider / Text GameObjects.
    ///   3. Connect each Button.onClick in the Inspector:
    ///        _playButton.onClick  → ReplayUI.OnPlayClicked
    ///        _pauseButton.onClick → ReplayUI.OnPauseClicked
    ///        _stopButton.onClick  → ReplayUI.OnStopClicked
    ///   4. Connect _scrubber.onValueChanged → ReplayUI.OnSliderChanged.
    ///   5. Add a VoidGameEventListener on the same GO, channel = ReplaySO._onReplayReady,
    ///      response → ReplayUI.OnReplayReady. This activates the panel when recording ends.
    /// </summary>
    [AddComponentMenu("BattleRobots/UI/Replay UI")]
    public sealed class ReplayUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("ReplaySO containing the recorded match snapshots.")]
        [SerializeField] private ReplaySO _replay;

        [Header("Playback Controls")]
        [Tooltip("Button that starts / resumes playback.")]
        [SerializeField] private Button _playButton;

        [Tooltip("Button that pauses playback without resetting the playhead.")]
        [SerializeField] private Button _pauseButton;

        [Tooltip("Button that stops playback and hides the panel.")]
        [SerializeField] private Button _stopButton;

        [Header("Scrubber")]
        [Tooltip("Slider representing the playhead position. " +
                 "maxValue is set to ReplaySO.TotalDuration when the panel opens.")]
        [SerializeField] private Slider _scrubber;

        [Tooltip("Text label displaying the current playhead time as 'M:SS / M:SS'.")]
        [SerializeField] private Text _timeLabel;

        // ── Runtime state ─────────────────────────────────────────────────────

        /// <summary>True while replay is playing (not paused or stopped).</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Current playhead position in seconds.</summary>
        public float PlayheadTime { get; private set; }

        /// <summary>The snapshot at the current playhead position. Updated by Seek/Advance.</summary>
        public MatchStateSnapshot CurrentSnapshot { get; private set; }

        // Flag to suppress OnSliderChanged callbacks while we reposition the slider in code.
        private bool _suppressSliderCallback;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Start hidden — panel activates via OnReplayReady.
            gameObject.SetActive(false);
        }

        // ── Button callbacks (wire in Inspector) ──────────────────────────────

        /// <summary>Begins or resumes replay playback.</summary>
        public void OnPlayClicked()
        {
            if (_replay == null || _replay.IsEmpty) return;
            IsPlaying = true;
            RefreshButtonStates();
        }

        /// <summary>Pauses playback; playhead position is preserved.</summary>
        public void OnPauseClicked()
        {
            IsPlaying = false;
            RefreshButtonStates();
        }

        /// <summary>Stops playback, resets playhead to 0, and hides the panel.</summary>
        public void OnStopClicked()
        {
            IsPlaying    = false;
            PlayheadTime = 0f;
            gameObject.SetActive(false);
        }

        // ── Slider callback (wire in Inspector) ───────────────────────────────

        /// <summary>
        /// Called by <c>_scrubber.onValueChanged</c> when the user drags the slider.
        /// Performs a single <see cref="ReplaySO.Seek"/> and updates the time label.
        /// Does NOT run per-frame — only fires on user interaction or programmatic value changes
        /// not suppressed by <see cref="_suppressSliderCallback"/>.
        /// </summary>
        public void OnSliderChanged(float value)
        {
            if (_suppressSliderCallback || _replay == null) return;

            IsPlaying    = false;
            PlayheadTime = value;
            CurrentSnapshot = _replay.Seek(value);
            UpdateTimeLabel();
            RefreshButtonStates();
        }

        // ── VoidGameEventListener callback ────────────────────────────────────

        /// <summary>
        /// Called by a VoidGameEventListener wired to <c>ReplaySO._onReplayReady</c>.
        /// Activates the panel and initialises the scrubber range.
        /// </summary>
        public void OnReplayReady()
        {
            if (_replay == null || _replay.IsEmpty) return;

            IsPlaying    = false;
            PlayheadTime = 0f;

            if (_scrubber != null)
            {
                _suppressSliderCallback = true;
                _scrubber.minValue = 0f;
                _scrubber.maxValue = _replay.TotalDuration;
                _scrubber.value    = 0f;
                _suppressSliderCallback = false;
            }

            CurrentSnapshot = _replay.Seek(0f);
            UpdateTimeLabel();
            RefreshButtonStates();
            gameObject.SetActive(true);
        }

        // ── Playback driver (call from scene controller each frame while IsPlaying) ─

        /// <summary>
        /// Advances the playhead by <paramref name="deltaTime"/> seconds.
        /// Intended to be called by an external scene controller (e.g. MatchManager or
        /// a dedicated ReplayController MB) in its Update, not by this component itself.
        /// Stops automatically when the end of the recording is reached.
        /// Zero allocation.
        /// </summary>
        public void AdvancePlayback(float deltaTime)
        {
            if (!IsPlaying || _replay == null || _replay.IsEmpty) return;

            PlayheadTime = Mathf.Min(PlayheadTime + deltaTime, _replay.TotalDuration);
            CurrentSnapshot = _replay.Seek(PlayheadTime);

            // Keep slider in sync without triggering OnSliderChanged.
            if (_scrubber != null)
            {
                _suppressSliderCallback = true;
                _scrubber.value = PlayheadTime;
                _suppressSliderCallback = false;
            }

            UpdateTimeLabel();

            // Auto-stop at end.
            if (Mathf.Approximately(PlayheadTime, _replay.TotalDuration))
            {
                IsPlaying = false;
                RefreshButtonStates();
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void UpdateTimeLabel()
        {
            if (_timeLabel == null || _replay == null) return;
            _timeLabel.text = $"{FormatTime(PlayheadTime)} / {FormatTime(_replay.TotalDuration)}";
        }

        private void RefreshButtonStates()
        {
            bool hasData = _replay != null && !_replay.IsEmpty;
            if (_playButton  != null) _playButton.interactable  = hasData && !IsPlaying;
            if (_pauseButton != null) _pauseButton.interactable = hasData && IsPlaying;
            if (_stopButton  != null) _stopButton.interactable  = hasData;
        }

        /// <summary>Formats seconds as M:SS. Public static for unit-test access.</summary>
        public static string FormatTime(float seconds)
        {
            int total   = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = total / 60;
            int secs    = total % 60;
            return $"{minutes}:{secs:D2}";
        }
    }
}
