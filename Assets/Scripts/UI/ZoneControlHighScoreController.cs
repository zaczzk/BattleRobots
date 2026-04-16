using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that records and displays the player's all-time best
    /// zone-control figures: best zone count, best pace, and best streak.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _bestZoneLabel  → "Best Zones: N".
    ///   _bestPaceLabel  → "Best Pace: F1/min".
    ///   _bestStreakLabel → "Best Streak: N".
    ///   _newZoneBadge   → Active when the latest match set a new zone record.
    ///   _newPaceBadge   → Active when the latest match set a new pace record.
    ///   _newStreakBadge → Active when the latest match set a new streak record.
    ///   _panel          → Root panel; hidden when <c>_highScoreSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to <c>_onMatchEnded</c> to trigger an update.
    ///   - Subscribes to <c>_onNewHighScore</c> for reactive refresh.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one high-score panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _highScoreSO → ZoneControlHighScoreSO asset.
    ///   2. Assign _summarySO   → ZoneControlSessionSummarySO (zone count + streak).
    ///   3. Assign _trackerSO   → ZoneCapturePaceTrackerSO (pace reading, optional).
    ///   4. Assign _onMatchEnded     → shared MatchEnded VoidGameEvent.
    ///   5. Assign _onNewHighScore   → highScoreSO._onNewHighScore channel.
    ///   6. Assign label / badge / panel UI refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlHighScoreController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlHighScoreSO          _highScoreSO;
        [SerializeField] private ZoneControlSessionSummarySO     _summarySO;
        [SerializeField] private ZoneCapturePaceTrackerSO        _trackerSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to the shared MatchEnded VoidGameEvent.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("Wire to ZoneControlHighScoreSO._onNewHighScore.")]
        [SerializeField] private VoidGameEvent _onNewHighScore;

        [Header("UI Refs — Labels (optional)")]
        [SerializeField] private Text _bestZoneLabel;
        [SerializeField] private Text _bestPaceLabel;
        [SerializeField] private Text _bestStreakLabel;

        [Header("UI Refs — New-Record Badges (optional)")]
        [SerializeField] private GameObject _newZoneBadge;
        [SerializeField] private GameObject _newPaceBadge;
        [SerializeField] private GameObject _newStreakBadge;

        [Header("UI Refs — Panel (optional)")]
        [SerializeField] private GameObject _panel;

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
            _onNewHighScore?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onNewHighScore?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads current match figures from the bound SOs, calls
        /// <see cref="ZoneControlHighScoreSO.UpdateFromMatch"/>, and refreshes the HUD.
        /// No-op when <c>_highScoreSO</c> is null.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_highScoreSO == null)
            {
                Refresh();
                return;
            }

            int   zoneCount = _summarySO != null ? _summarySO.TotalZonesCaptured : 0;
            float pace      = _trackerSO  != null ? _trackerSO.GetCapturesPerMinute(UnityEngine.Time.time) : 0f;
            int   streak    = _summarySO  != null ? _summarySO.BestCaptureStreak  : 0;

            _highScoreSO.UpdateFromMatch(zoneCount, pace, streak);
            Refresh();
        }

        /// <summary>
        /// Rebuilds all HUD elements from the current high-score SO state.
        /// Hides the panel when <c>_highScoreSO</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_highScoreSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bestZoneLabel != null)
                _bestZoneLabel.text  = $"Best Zones: {_highScoreSO.BestZoneCount}";

            if (_bestPaceLabel != null)
                _bestPaceLabel.text  = $"Best Pace: {_highScoreSO.BestPace:F1}/min";

            if (_bestStreakLabel != null)
                _bestStreakLabel.text = $"Best Streak: {_highScoreSO.BestStreak}";

            _newZoneBadge?.SetActive(_highScoreSO.IsNewZoneCount);
            _newPaceBadge?.SetActive(_highScoreSO.IsNewPace);
            _newStreakBadge?.SetActive(_highScoreSO.IsNewStreak);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound high-score SO (may be null).</summary>
        public ZoneControlHighScoreSO HighScoreSO => _highScoreSO;

        /// <summary>The bound session summary SO (may be null).</summary>
        public ZoneControlSessionSummarySO SummarySO => _summarySO;
    }
}
