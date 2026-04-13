using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Fires a "sudden death" event once when the match timer first crosses a
    /// configurable threshold, then stays silent until the next match reset.
    ///
    /// ── Responsibilities ─────────────────────────────────────────────────────
    ///   • Subscribes to the match timer's <see cref="_onTimerUpdated"/> FloatGameEvent
    ///     (same channel driven by <c>MatchManager._onTimerUpdated</c>).
    ///   • The first time <c>secondsRemaining ≤ _triggerThreshold</c>, sets
    ///     <see cref="IsSuddenDeathActive"/> and raises <c>_onSuddenDeathStarted</c>.
    ///   • Subsequent timer events are ignored until <see cref="ResetState"/> is called
    ///     (automatically wired to <c>_onMatchStarted</c>).
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   1. Add this MB to any persistent scene GO (e.g., MatchManager root).
    ///   2. <c>_onTimerUpdated</c> → the <c>FloatGameEvent</c> in MatchManager
    ///      (same SO as MatchTimerWarningController uses).
    ///   3. <c>_onMatchStarted</c> → the VoidGameEvent raised at match start,
    ///      so sudden death resets between matches.
    ///   4. <c>_onSuddenDeathStarted</c> → a VoidGameEvent wired to
    ///      AudioManager, VFX handlers, or a UI overlay.
    ///   5. <c>_triggerThreshold</c> → seconds remaining when sudden death kicks in
    ///      (default 30 s — one full "danger period" before time-out).
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.Core namespace — no Physics or UI references.
    ///   • Action delegates cached in Awake — no heap allocation in event handlers.
    ///   • IsSuddenDeathActive is the canonical single-source-of-truth flag.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchSuddenDeathController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels In")]
        [Tooltip("FloatGameEvent raised each second by MatchManager carrying seconds remaining. " +
                 "Wire to MatchManager._onTimerUpdated.")]
        [SerializeField] private FloatGameEvent _onTimerUpdated;

        [Tooltip("VoidGameEvent raised at match start (used to reset sudden-death state " +
                 "so each match begins fresh). Leave null if matches do not loop.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("Event Channel Out")]
        [Tooltip("Raised exactly once per match when the timer crosses _triggerThreshold. " +
                 "Wire to AudioManager, VFX handlers, or a UI sudden-death overlay.")]
        [SerializeField] private VoidGameEvent _onSuddenDeathStarted;

        [Header("Threshold")]
        [Tooltip("Seconds remaining at which sudden death activates. " +
                 "When the timer first reaches or drops below this value the event fires.")]
        [SerializeField, Min(0f)] private float _triggerThreshold = 30f;

        // ── Runtime state (not serialized) ────────────────────────────────────

        private bool _suddenDeathActive;

        private Action<float> _timerDelegate;
        private Action        _resetDelegate;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>
        /// True once the timer has crossed <c>_triggerThreshold</c> this match.
        /// Resets to false at match start via <c>_onMatchStarted</c>.
        /// </summary>
        public bool IsSuddenDeathActive => _suddenDeathActive;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _timerDelegate = OnTimerUpdated;
            _resetDelegate = ResetState;
        }

        private void OnEnable()
        {
            _onTimerUpdated?.RegisterCallback(_timerDelegate);
            _onMatchStarted?.RegisterCallback(_resetDelegate);
            ResetState();
        }

        private void OnDisable()
        {
            _onTimerUpdated?.UnregisterCallback(_timerDelegate);
            _onMatchStarted?.UnregisterCallback(_resetDelegate);
        }

        // ── Event handlers ────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates whether sudden death should trigger.
        /// Fires at most once per match; subsequent calls are no-ops until ResetState().
        /// No allocation — float comparison only.
        /// </summary>
        private void OnTimerUpdated(float secondsRemaining)
        {
            if (_suddenDeathActive) return;

            if (secondsRemaining <= _triggerThreshold)
            {
                _suddenDeathActive = true;
                _onSuddenDeathStarted?.Raise();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Clears the sudden-death flag so the next match begins in normal mode.
        /// Called automatically via <c>_onMatchStarted</c>; also callable directly
        /// (e.g., from MatchManager or EditMode tests).
        /// Allocation-free.
        /// </summary>
        public void ResetState()
        {
            _suddenDeathActive = false;
        }
    }
}
