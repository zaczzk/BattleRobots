using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that evaluates and displays a <see cref="ZoneControlWeeklyChallengeSO"/>
    /// at the end of each match.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _progressBar    → filled 0–1 from <see cref="ZoneControlWeeklyChallengeSO.Progress"/>.
    ///   _progressLabel  → "N% Complete" or "Complete!" when IsComplete.
    ///   _completeBadge  → active only when <see cref="ZoneControlWeeklyChallengeSO.IsComplete"/>.
    ///   _panel          → Root panel; hidden when _challengeSO is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to _onMatchEnded to trigger evaluation and Refresh.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one challenge panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _challengeSO  → ZoneControlWeeklyChallengeSO asset.
    ///   2. Assign _summarySO    → ZoneControlSessionSummarySO asset.
    ///   3. Assign _onMatchEnded → shared MatchEnded VoidGameEvent.
    ///   4. Assign UI refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlWeeklyChallengeController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlWeeklyChallengeSO _challengeSO;
        [SerializeField] private ZoneControlSessionSummarySO  _summarySO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to the shared MatchEnded VoidGameEvent.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [Tooltip("Wire to the challenge's own ChallengeComplete VoidGameEvent.")]
        [SerializeField] private VoidGameEvent _onChallengeComplete;

        [Header("UI Refs (optional)")]
        [SerializeField] private Slider      _progressBar;
        [SerializeField] private Text        _progressLabel;
        [SerializeField] private GameObject  _completeBadge;
        [SerializeField] private GameObject  _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
            _refreshDelegate          = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onChallengeComplete?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onChallengeComplete?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates the challenge from current session summary data, then refreshes
        /// the HUD. Called automatically when <c>_onMatchEnded</c> fires.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_challengeSO == null || _summarySO == null)
            {
                Refresh();
                return;
            }

            float value;
            switch (_challengeSO.ChallengeType)
            {
                case ZoneControlChallengeType.Streak:
                    value = _summarySO.BestCaptureStreak;
                    break;
                case ZoneControlChallengeType.ZoneCount:
                default:
                    value = _summarySO.TotalZonesCaptured;
                    break;
            }

            _challengeSO.EvaluateChallenge(value);
            Refresh();
        }

        /// <summary>
        /// Rebuilds all HUD elements from the current challenge SO state.
        /// Hides the panel when <c>_challengeSO</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_challengeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_progressBar != null)
                _progressBar.value = _challengeSO.Progress;

            if (_progressLabel != null)
            {
                _progressLabel.text = _challengeSO.IsComplete
                    ? "Complete!"
                    : $"{Mathf.RoundToInt(_challengeSO.Progress * 100f)}% Complete";
            }

            _completeBadge?.SetActive(_challengeSO.IsComplete);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound weekly challenge SO (may be null).</summary>
        public ZoneControlWeeklyChallengeSO ChallengeSO => _challengeSO;

        /// <summary>The bound session summary SO (may be null).</summary>
        public ZoneControlSessionSummarySO SummarySO => _summarySO;
    }
}
