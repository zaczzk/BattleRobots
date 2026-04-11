using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays live progress toward the active daily challenge during a match.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   • Listens to <c>_onMatchStarted</c> / <c>_onMatchEnded</c> to show/hide
    ///     the challenge panel.
    ///   • Listens to <c>_onTimerUpdated</c> (FloatGameEvent, payload = seconds
    ///     remaining) to compute elapsed time and refresh the progress display
    ///     once per integer second (dedup guard — no-alloc steady-state).
    ///   • Reads live damage stats from <c>_matchStatistics</c> on each tick
    ///     so damage-based conditions are always current.
    ///
    /// ── Zero-alloc strategy ──────────────────────────────────────────────────
    ///   All Action delegates are cached in Awake.  The progress string is only
    ///   rebuilt when the deduplicated elapsed-second counter changes
    ///   (≤1 string alloc/second while a match is running — acceptable for UI).
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Place this MB on a Canvas child alongside CombatHUDController.
    ///   2. Assign _dailyChallenge → DailyChallengeSO (same one used by
    ///      DailyChallengeManager).
    ///   3. Assign _matchStatistics → MatchStatisticsSO (same one used by
    ///      MatchManager).
    ///   4. Wire event channels:
    ///        _onMatchStarted → MatchStarted VoidGameEvent SO
    ///        _onMatchEnded   → MatchEnded VoidGameEvent SO
    ///        _onTimerUpdated → FloatGameEvent SO
    ///                          (same asset as MatchManager._onTimerUpdated)
    ///   5. Optionally assign _challengePanel, _challengeNameText,
    ///      _challengeProgressText, _completedText.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — no BattleRobots.Physics references.
    ///   - No Update / FixedUpdate — purely event-driven.
    ///   - Panel hidden by default; shown only while match is running and a
    ///     valid daily challenge exists.
    /// </summary>
    public sealed class ChallengeProgressHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data Sources (optional)")]
        [Tooltip("Runtime blackboard that holds today's daily challenge.")]
        [SerializeField] private DailyChallengeSO _dailyChallenge;

        [Tooltip("Runtime accumulator for per-match damage statistics. " +
                 "Assign the same MatchStatisticsSO used by MatchManager.")]
        [SerializeField] private MatchStatisticsSO _matchStatistics;

        [Header("UI (optional)")]
        [Tooltip("Root panel shown only while a match is running and a challenge is active. " +
                 "Hidden automatically by Awake and HandleMatchEnded.")]
        [SerializeField] private GameObject _challengePanel;

        [Tooltip("Text label showing the challenge display name.")]
        [SerializeField] private Text _challengeNameText;

        [Tooltip("Text label showing live progress, e.g. '47 / 80' or '12s / 30s'.")]
        [SerializeField] private Text _challengeProgressText;

        [Tooltip("Text label shown when the daily challenge has already been completed. " +
                 "Cleared when the challenge is still in progress.")]
        [SerializeField] private Text _completedText;

        [Header("Event Channels — In")]
        [Tooltip("VoidGameEvent raised when the match begins (same SO as MatchManager).")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("VoidGameEvent raised when the match ends.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("FloatGameEvent raised by MatchManager every frame; payload = seconds remaining. " +
                 "The first value emitted after MatchStarted is used as the round duration.")]
        [SerializeField] private FloatGameEvent _onTimerUpdated;

        // ── Cached delegates (zero-alloc OnEnable/OnDisable) ─────────────────

        private Action        _matchStartedDelegate;
        private Action        _matchEndedDelegate;
        private Action<float> _timerUpdatedDelegate;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool  _matchRunning;
        private float _roundDuration;
        private bool  _durationCaptured;
        private int   _lastElapsedInt = -1;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _matchStartedDelegate = HandleMatchStarted;
            _matchEndedDelegate   = HandleMatchEnded;
            _timerUpdatedDelegate = HandleTimerUpdated;

            if (_challengePanel != null) _challengePanel.SetActive(false);
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_matchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_matchEndedDelegate);
            _onTimerUpdated?.RegisterCallback(_timerUpdatedDelegate);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_matchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_matchEndedDelegate);
            _onTimerUpdated?.UnregisterCallback(_timerUpdatedDelegate);
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void HandleMatchStarted()
        {
            _matchRunning     = true;
            _durationCaptured = false;
            _lastElapsedInt   = -1;
            Refresh(0f);
        }

        private void HandleMatchEnded()
        {
            _matchRunning = false;
            if (_challengePanel != null) _challengePanel.SetActive(false);
        }

        private void HandleTimerUpdated(float secondsRemaining)
        {
            if (!_matchRunning) return;

            // Capture the round duration from the first timer value emitted after
            // match start. MatchManager.HandleMatchStarted raises _onTimerUpdated
            // with the full _timeRemaining immediately, so the first value received
            // here equals the configured round duration.
            if (!_durationCaptured)
            {
                _roundDuration    = secondsRemaining;
                _durationCaptured = true;
            }

            float elapsed    = _roundDuration - secondsRemaining;
            int   elapsedInt = Mathf.FloorToInt(elapsed);

            // Dedup: only rebuild the progress string once per integer second to
            // avoid a per-frame string allocation (≤1 alloc/second — same strategy
            // as CombatHUDController).
            if (elapsedInt == _lastElapsedInt) return;
            _lastElapsedInt = elapsedInt;

            Refresh(elapsed);
        }

        // ── Core refresh logic ────────────────────────────────────────────────

        private void Refresh(float elapsedSeconds)
        {
            if (_dailyChallenge == null) return;

            var  challenge    = _dailyChallenge.CurrentChallenge;
            bool hasChallenge = challenge != null;

            // Show the panel only while the match is running and there is an active
            // challenge to display.
            if (_challengePanel != null)
                _challengePanel.SetActive(_matchRunning && hasChallenge);

            if (!hasChallenge) return;

            // Challenge name
            if (_challengeNameText != null)
                _challengeNameText.text = challenge.DisplayName;

            // Completed state: replace progress text with a "complete" label.
            if (_dailyChallenge.IsCompleted)
            {
                if (_challengeProgressText != null) _challengeProgressText.text = "";
                if (_completedText         != null) _completedText.text         = "COMPLETE!";
                return;
            }

            if (_completedText != null) _completedText.text = "";

            // Build and apply the live progress string.
            if (_challengeProgressText != null)
            {
                float damageDone  = _matchStatistics != null
                    ? _matchStatistics.TotalDamageDealt  : 0f;
                float damageTaken = _matchStatistics != null
                    ? _matchStatistics.TotalDamageTaken  : 0f;

                _challengeProgressText.text =
                    BuildProgressText(challenge, elapsedSeconds, damageDone, damageTaken);
            }
        }

        // ── Testable progress-text builder ────────────────────────────────────

        /// <summary>
        /// Builds a human-readable progress string for the given
        /// <paramref name="condition"/> using live match statistics.
        ///
        /// Marked <c>internal</c> so EditMode tests can call it directly without
        /// needing a full MonoBehaviour lifecycle.
        ///
        /// Returns <see cref="string.Empty"/> when <paramref name="condition"/> is null.
        /// </summary>
        /// <param name="condition">The active bonus condition to display progress for.</param>
        /// <param name="elapsedSeconds">Seconds elapsed since match start.</param>
        /// <param name="damageDone">Total damage dealt by the player this match.</param>
        /// <param name="damageTaken">Total damage taken by the player this match.</param>
        internal static string BuildProgressText(
            BonusConditionSO condition,
            float            elapsedSeconds,
            float            damageDone,
            float            damageTaken)
        {
            if (condition == null) return "";

            switch (condition.ConditionType)
            {
                case BonusConditionType.NoDamageTaken:
                    return damageTaken <= condition.Threshold
                        ? $"No Damage: {(int)damageTaken} taken"
                        : $"No Damage: FAILED ({(int)damageTaken} taken)";

                case BonusConditionType.WonUnderDuration:
                    return $"Speed Run: {(int)elapsedSeconds}s / {(int)condition.Threshold}s";

                case BonusConditionType.DamageDealtExceeds:
                    return $"Damage: {(int)damageDone} / {(int)condition.Threshold}";

                case BonusConditionType.DamageEfficiency:
                {
                    float total = damageDone + damageTaken;
                    float eff   = total > 0f ? damageDone / total : 0f;
                    return $"Efficiency: {(int)(eff * 100)}% / {(int)(condition.Threshold * 100)}%";
                }

                default:
                    return condition.DisplayName;
            }
        }
    }
}
