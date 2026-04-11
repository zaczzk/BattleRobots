using System.Collections;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Pre-match countdown: fires <see cref="IntGameEvent"/> ticks
    /// (<c>_countdownFrom</c>, …, 1) then raises <c>_onCountdownComplete</c>
    /// and <c>_matchStartedEvent</c>.
    ///
    /// ── Execution paths ───────────────────────────────────────────────────────
    ///   • <c>_countdownFrom ≤ 0</c>                  — fires complete + MatchStarted
    ///     immediately; no ticks emitted.
    ///   • <c>_countdownFrom &gt; 0, any delay &gt; 0</c> — coroutine; ticks fire every
    ///     <c>_tickInterval</c> seconds after an optional <c>_initialDelay</c>.
    ///   • <c>_countdownFrom &gt; 0, both delays == 0</c> — synchronous; all ticks fire
    ///     in the same frame (enables EditMode testing without coroutine plumbing).
    ///
    /// ── Drop-in for MatchStarter ───────────────────────────────────────────────
    ///   Wire <c>_matchStartedEvent</c> to the same VoidGameEvent SO used by
    ///   MatchManager, MatchFlowController, ArenaManager, PauseManager, and
    ///   CombatHUDController — then remove MatchStarter from the scene.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Add this component to any persistent Arena-scene GameObject (e.g., GameManager).
    ///   2. Assign <c>_matchStartedEvent</c> (required).
    ///   3. Create an IntGameEvent SO for <c>_onCountdownTick</c>; wire it to
    ///      <see cref="BattleRobots.UI.MatchCountdownController._onCountdownTick"/>.
    ///   4. Create a VoidGameEvent SO for <c>_onCountdownComplete</c>; wire it to
    ///      <see cref="BattleRobots.UI.MatchCountdownController._onCountdownComplete"/>.
    ///   5. Tune <c>_countdownFrom</c> (default 3) and <c>_tickInterval</c> (default 1 s).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Raises exactly once per scene load.
    ///   - Zero allocations in the coroutine hot path (only yield objects).
    /// </summary>
    public sealed class CountdownManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Countdown Settings")]
        [Tooltip("Number of visible tick values (e.g., 3 → emits ticks: 3, 2, 1). " +
                 "Set to 0 for an immediate start with no countdown display.")]
        [SerializeField, Min(0)] private int _countdownFrom = 3;

        [Tooltip("Seconds between consecutive ticks. Set to 0 for an instantaneous sequence.")]
        [SerializeField, Min(0f)] private float _tickInterval = 1f;

        [Tooltip("Seconds to wait after Start() before the first tick fires. " +
                 "A small value (e.g. 0.1 s) lets ArticulationBody physics settle.")]
        [SerializeField, Min(0f)] private float _initialDelay = 0.1f;

        [Header("Event Channels — Out")]
        [Tooltip("Raised once per countdown step. Payload = current count value (e.g., 3, 2, 1). " +
                 "Wire to MatchCountdownController._onCountdownTick.")]
        [SerializeField] private IntGameEvent _onCountdownTick;

        [Tooltip("Raised after the last tick, just before _matchStartedEvent. " +
                 "Use as the 'FIGHT!' display trigger in MatchCountdownController.")]
        [SerializeField] private VoidGameEvent _onCountdownComplete;

        [Tooltip("Required. The MatchStarted VoidGameEvent SO shared with MatchManager, " +
                 "MatchFlowController, ArenaManager, PauseManager, and CombatHUDController. " +
                 "Raised immediately after _onCountdownComplete.")]
        [SerializeField] private VoidGameEvent _matchStartedEvent;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            if (_matchStartedEvent == null)
            {
                Debug.LogError("[CountdownManager] _matchStartedEvent is not assigned — " +
                               "MatchStarted will never fire. Assign the SO in the Inspector.");
                return;
            }

            if (_countdownFrom <= 0)
            {
                // Zero/negative countdown: skip straight to complete + start.
                FireCompleteAndStart();
                return;
            }

            // Use synchronous path when both timing fields are zero — avoids
            // coroutine overhead in tests and zero-delay wiring scenarios.
            bool needsCoroutine = _initialDelay > 0f || _tickInterval > 0f;
            if (needsCoroutine)
                StartCoroutine(CountdownCoroutine());
            else
                RunSynchronousCountdown();
        }

        // ── Private ───────────────────────────────────────────────────────────

        /// <summary>
        /// Fires all ticks and complete in the same frame (zero-delay path).
        /// Active when <c>_countdownFrom &gt; 0</c> and both delay fields are zero.
        /// </summary>
        private void RunSynchronousCountdown()
        {
            for (int i = _countdownFrom; i >= 1; i--)
                _onCountdownTick?.Raise(i);

            FireCompleteAndStart();
        }

        private IEnumerator CountdownCoroutine()
        {
            if (_initialDelay > 0f)
                yield return new WaitForSeconds(_initialDelay);

            for (int i = _countdownFrom; i >= 1; i--)
            {
                _onCountdownTick?.Raise(i);
                if (_tickInterval > 0f)
                    yield return new WaitForSeconds(_tickInterval);
            }

            FireCompleteAndStart();
        }

        private void FireCompleteAndStart()
        {
            _onCountdownComplete?.Raise();
            _matchStartedEvent.Raise();
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_matchStartedEvent == null)
                Debug.LogWarning("[CountdownManager] _matchStartedEvent VoidGameEvent SO " +
                                 "not assigned — MatchStarted will never fire.", this);
        }
#endif
    }
}
