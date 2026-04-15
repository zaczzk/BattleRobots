using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays post-match and career zone-control score totals
    /// from a <see cref="ZoneScoreTrackerSO"/>.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _panel               → root container; hidden when _tracker is null.
    ///   _currentPlayerLabel  → "Match P: N" (current-match player zone score).
    ///   _currentEnemyLabel   → "Match E: N" (current-match enemy zone score).
    ///   _careerPlayerLabel   → "Career P: N" (cumulative career player zone score).
    ///   _careerEnemyLabel    → "Career E: N" (cumulative career enemy zone score).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes _onMatchEnded → Refresh so scores update reactively.
    ///   - ZoneCareerPersistenceController (Core MB) must handle AccumulateToCareer
    ///     + SaveData persistence before Refresh is called. Wire both to _onMatchEnded
    ///     and ensure the Core MB's Script Execution Order runs first.
    ///   - All UI refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _tracker      → the ZoneScoreTrackerSO asset.
    ///   2. Assign _onMatchEnded → MatchManager._onMatchEnded channel.
    ///   3. Assign optional UI labels and panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PostMatchZoneStatsController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneScoreTrackerSO _tracker;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _currentPlayerLabel;
        [SerializeField] private Text       _currentEnemyLabel;
        [SerializeField] private Text       _careerPlayerLabel;
        [SerializeField] private Text       _careerEnemyLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_refreshDelegate);
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_refreshDelegate);
            _panel?.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds all stat labels from the current <see cref="_tracker"/> state.
        /// Hides the panel when <c>_tracker</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_tracker == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_currentPlayerLabel != null)
                _currentPlayerLabel.text = $"Match P: {Mathf.RoundToInt(_tracker.PlayerScore)}";

            if (_currentEnemyLabel != null)
                _currentEnemyLabel.text = $"Match E: {Mathf.RoundToInt(_tracker.EnemyScore)}";

            if (_careerPlayerLabel != null)
                _careerPlayerLabel.text = $"Career P: {Mathf.RoundToInt(_tracker.CareerPlayerScore)}";

            if (_careerEnemyLabel != null)
                _careerEnemyLabel.text = $"Career E: {Mathf.RoundToInt(_tracker.CareerEnemyScore)}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneScoreTrackerSO"/>. May be null.</summary>
        public ZoneScoreTrackerSO Tracker => _tracker;
    }
}
