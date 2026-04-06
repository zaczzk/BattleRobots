using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Manages game-pause state via <see cref="Time.timeScale"/>.
    ///
    /// Responsibilities
    ///   • Toggles pause on ESC (checked once per frame in Update — single key poll).
    ///   • Exposes <see cref="Pause"/>, <see cref="Resume"/>, <see cref="TogglePause"/>
    ///     so UI buttons can drive state without reading Time.timeScale directly.
    ///   • Fires SO event channels so PauseMenuUI and other systems stay decoupled.
    ///   • Restores <c>Time.timeScale = 1</c> in OnDestroy to avoid a stale frozen game
    ///     if the scene is unloaded while paused.
    ///
    /// Architecture constraints
    ///   • <c>BattleRobots.Core</c> namespace — no Physics / UI dependencies.
    ///   • No heap allocations in Update (key poll returns bool; no string ops).
    ///   • DO NOT put multiple PauseManager components in a scene — guard via
    ///     <see cref="DisallowMultipleComponent"/>.
    ///
    /// Wire-up (Inspector)
    ///   1. Assign <c>_onPause</c> and <c>_onResume</c> VoidGameEvent SO assets.
    ///   2. (Optional) Set <c>_pauseKey</c> if you want a key other than Escape.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Key that toggles pause. Defaults to Escape.")]
        [SerializeField] private KeyCode _pauseKey = KeyCode.Escape;

        [Header("Event Channels")]
        [Tooltip("Raised when the game transitions from running → paused.")]
        [SerializeField] private VoidGameEvent _onPause;

        [Tooltip("Raised when the game transitions from paused → running.")]
        [SerializeField] private VoidGameEvent _onResume;

        // ── Runtime State ─────────────────────────────────────────────────────

        /// <summary>True while the game is paused.</summary>
        public bool IsPaused { get; private set; }

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            // Single key-down poll — no allocation.
            if (Input.GetKeyDown(_pauseKey))
                TogglePause();
        }

        private void OnDestroy()
        {
            // Safety: unfreeze time when the scene tears down.
            if (IsPaused)
                Time.timeScale = 1f;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Pauses the game. No-op if already paused.
        /// Sets <see cref="Time.timeScale"/> to 0 and fires <c>_onPause</c>.
        /// </summary>
        public void Pause()
        {
            if (IsPaused) return;

            IsPaused         = true;
            Time.timeScale   = 0f;
            _onPause?.Raise();
        }

        /// <summary>
        /// Resumes the game. No-op if not currently paused.
        /// Restores <see cref="Time.timeScale"/> to 1 and fires <c>_onResume</c>.
        /// </summary>
        public void Resume()
        {
            if (!IsPaused) return;

            IsPaused         = false;
            Time.timeScale   = 1f;
            _onResume?.Raise();
        }

        /// <summary>Toggles between paused and running states.</summary>
        public void TogglePause()
        {
            if (IsPaused) Resume();
            else          Pause();
        }
    }
}
