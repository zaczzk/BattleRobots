using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that evaluates zone-control achievements at match end and
    /// displays the earned count and latest achievement name.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _countLabel   → "N / Total earned" (e.g. "3 / 5").
    ///   _latestLabel  → Display name of the most recently earned achievement.
    ///   _panel        → Root panel; hidden when <c>_catalogSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to <c>_onMatchEnded</c> to trigger evaluation.
    ///   - Subscribes to <c>_onAchievementUnlocked</c> for reactive refresh.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one achievement panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _catalogSO → ZoneControlAchievementCatalogSO asset.
    ///   2. Assign _summarySO → ZoneControlSessionSummarySO asset.
    ///   3. Assign _onMatchEnded → shared MatchEnded VoidGameEvent.
    ///   4. Assign _onAchievementUnlocked → catalogSO._onAchievementUnlocked channel.
    ///   5. Assign _countLabel, _latestLabel, and _panel UI refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlAchievementController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlAchievementCatalogSO _catalogSO;
        [SerializeField] private ZoneControlSessionSummarySO     _summarySO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to the shared MatchEnded VoidGameEvent.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("Wire to ZoneControlAchievementCatalogSO._onAchievementUnlocked.")]
        [SerializeField] private VoidGameEvent _onAchievementUnlocked;

        [Header("UI Refs (optional)")]
        [Tooltip("Shows 'N / Total' earned achievement count.")]
        [SerializeField] private Text        _countLabel;

        [Tooltip("Shows the display name of the most recently earned achievement.")]
        [SerializeField] private Text        _latestLabel;

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
            _onAchievementUnlocked?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onAchievementUnlocked?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates all achievements against the current session summary, then
        /// refreshes the HUD. Called automatically when <c>_onMatchEnded</c> fires.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_catalogSO != null && _summarySO != null)
                _catalogSO.EvaluateAchievements(_summarySO);
            Refresh();
        }

        /// <summary>
        /// Rebuilds all HUD elements from the current catalog SO state.
        /// Hides the panel when <c>_catalogSO</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_catalogSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_countLabel != null)
                _countLabel.text = $"{_catalogSO.EarnedCount} / {_catalogSO.TotalCount}";

            if (_latestLabel != null)
                _latestLabel.text = _catalogSO.LatestEarnedDisplayName;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound achievement catalog SO (may be null).</summary>
        public ZoneControlAchievementCatalogSO CatalogSO => _catalogSO;

        /// <summary>The bound session summary SO (may be null).</summary>
        public ZoneControlSessionSummarySO SummarySO => _summarySO;
    }
}
