using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays a daily zone-control challenge and a
    /// countdown label showing how long until the challenge resets.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _targetLabel    → "Target: N" showing today's target value.
    ///   _countdownLabel → "Resets in Xh Ym" (updated every frame via Update).
    ///   _panel          → Root panel; hidden when <c>_config</c> is null.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   On match end, reads today's target from <c>_config</c>, optionally writes
    ///   it to <c>_challengeSO</c> as the evaluation target, and refreshes the HUD.
    ///   The countdown is refreshed every frame through Update.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to <c>_onMatchEnded</c> to apply the daily target.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one daily challenge panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _config        → ZoneControlDailyChallengeConfig asset.
    ///   2. Assign _challengeSO   → ZoneControlWeeklyChallengeSO asset (optional).
    ///   3. Assign _onMatchEnded  → shared MatchEnded VoidGameEvent.
    ///   4. Assign _targetLabel, _countdownLabel, and _panel UI refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlDailyChallengeController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlDailyChallengeConfig _config;
        [SerializeField] private ZoneControlWeeklyChallengeSO    _challengeSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to the shared MatchEnded VoidGameEvent.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("UI Refs (optional)")]
        [Tooltip("Shows the daily target value.")]
        [SerializeField] private Text       _targetLabel;

        [Tooltip("Shows the time remaining until the daily challenge resets.")]
        [SerializeField] private Text       _countdownLabel;

        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
        }

        private void Update()
        {
            // Refresh countdown every frame (time.deltaTime-driven; no alloc).
            if (_config != null && _countdownLabel != null)
                _countdownLabel.text = FormatCountdown(_config.GetSecondsUntilReset());
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads today's target from the config and optionally applies it to
        /// <c>_challengeSO</c> as the evaluation target, then refreshes the HUD.
        /// No-op when <c>_config</c> is null.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_config == null)
            {
                Refresh();
                return;
            }

            float todayTarget = _config.GetTodayTarget();

            if (_challengeSO != null)
                _challengeSO.EvaluateChallenge(todayTarget);

            Refresh();
        }

        /// <summary>
        /// Rebuilds the target label and panel visibility from the current config.
        /// Hides the panel when <c>_config</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_config == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_targetLabel != null)
                _targetLabel.text = $"Target: {_config.GetTodayTarget():F0}";

            if (_countdownLabel != null)
                _countdownLabel.text = FormatCountdown(_config.GetSecondsUntilReset());
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Formats <paramref name="totalSeconds"/> as "Resets in Xh Ym".
        /// Zero alloc at runtime via integer arithmetic only.
        /// </summary>
        internal static string FormatCountdown(int totalSeconds)
        {
            if (totalSeconds <= 0) return "Resets soon";
            int hours   = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            return $"Resets in {hours}h {minutes}m";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound daily challenge config (may be null).</summary>
        public ZoneControlDailyChallengeConfig Config => _config;

        /// <summary>The bound weekly challenge SO used for evaluation (may be null).</summary>
        public ZoneControlWeeklyChallengeSO ChallengeSO => _challengeSO;
    }
}
