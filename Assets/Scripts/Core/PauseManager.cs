using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Manages pause state during a match: detects Escape key, gates
    /// <c>Time.timeScale</c>, and fires SO event channels so the UI layer
    /// can show/hide the pause panel without a direct dependency on this class.
    ///
    /// ── Pause lifecycle ───────────────────────────────────────────────────────
    ///   1. Match begins  → subscribes to <c>_onMatchStarted</c>; sets _matchRunning.
    ///   2. Escape pressed while match is running → TogglePause().
    ///   3. Match ends    → auto-resumes if paused (results screen takes over).
    ///
    /// ── Time.timeScale contract ───────────────────────────────────────────────
    ///   Paused  → 0   (Physics + FixedUpdate frozen; Input still works in Update).
    ///   Unpaused → 1  (restored on Resume / match end).
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Add to any persistent Arena-scene GameObject.
    ///   2. Assign _onMatchStarted / _onMatchEnded VoidGameEvent SOs.
    ///   3. Assign _onPaused / _onResumed VoidGameEvent SOs.
    ///   4. Wire PauseMenuController._onPaused and ._onResumed to the same SOs.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace. No Physics / UI references.
    ///   - Escape key checked in Update (allocation-free KeyCode comparison).
    ///   - Delegates cached in Awake — zero alloc on Subscribe/Unsubscribe.
    /// </summary>
    public sealed class PauseManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("VoidGameEvent raised when a match begins. Enables pause detection.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("VoidGameEvent raised when a match ends. Disables pause and auto-resumes.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("Event Channels — Out")]
        [Tooltip("Raised when the game is paused. Subscribe in PauseMenuController to show panel.")]
        [SerializeField] private VoidGameEvent _onPaused;

        [Tooltip("Raised when the game is resumed. Subscribe in PauseMenuController to hide panel.")]
        [SerializeField] private VoidGameEvent _onResumed;

        // ── Runtime state ─────────────────────────────────────────────────────

        /// <summary>True while the game is paused.</summary>
        public bool IsPaused => _isPaused;

        private bool _isPaused;
        private bool _matchRunning;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _matchStartedCallback;
        private Action _matchEndedCallback;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _matchStartedCallback = HandleMatchStarted;
            _matchEndedCallback   = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_matchStartedCallback);
            _onMatchEnded?.RegisterCallback(_matchEndedCallback);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_matchStartedCallback);
            _onMatchEnded?.UnregisterCallback(_matchEndedCallback);

            // Safety: restore timeScale if this component is disabled mid-match.
            if (_isPaused) ForceResume();
        }

        private void Update()
        {
            // Pause only allowed while a match is running.
            if (!_matchRunning) return;

            // Input.GetKeyDown is allocation-free.
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Toggle between paused and unpaused.</summary>
        public void TogglePause()
        {
            if (_isPaused) Resume();
            else           Pause();
        }

        /// <summary>
        /// Pauses the game: sets <c>Time.timeScale</c> to 0 and fires <c>_onPaused</c>.
        /// No-ops if already paused.
        /// </summary>
        public void Pause()
        {
            if (_isPaused) return;

            _isPaused      = true;
            Time.timeScale = 0f;
            _onPaused?.Raise();

            Debug.Log("[PauseManager] Game paused.");
        }

        /// <summary>
        /// Resumes the game: restores <c>Time.timeScale</c> to 1 and fires <c>_onResumed</c>.
        /// No-ops if not currently paused.
        /// </summary>
        public void Resume()
        {
            if (!_isPaused) return;

            ForceResume();
            Debug.Log("[PauseManager] Game resumed.");
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void HandleMatchStarted()
        {
            _matchRunning  = true;
            _isPaused      = false;
            Time.timeScale = 1f; // ensure unpaused at match start
        }

        private void HandleMatchEnded()
        {
            _matchRunning = false;

            if (_isPaused)
            {
                ForceResume();
                Debug.Log("[PauseManager] Auto-resumed on match end.");
            }
        }

        /// <summary>
        /// Internal resume that fires _onResumed regardless of current state.
        /// Used by HandleMatchEnded and OnDisable to guarantee timeScale restoration.
        /// </summary>
        private void ForceResume()
        {
            _isPaused      = false;
            Time.timeScale = 1f;
            _onResumed?.Raise();
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_onMatchStarted == null)
                Debug.LogWarning("[PauseManager] _onMatchStarted not assigned.", this);
            if (_onMatchEnded == null)
                Debug.LogWarning("[PauseManager] _onMatchEnded not assigned.", this);
            if (_onPaused == null)
                Debug.LogWarning("[PauseManager] _onPaused not assigned.", this);
            if (_onResumed == null)
                Debug.LogWarning("[PauseManager] _onResumed not assigned.", this);
        }
#endif
    }
}
